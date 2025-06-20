using QuickFix;
using QuickFix.Fields;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;
using System;
using System.Threading.Tasks;

public class FixClient : QuickFix.MessageCracker, IApplication
{
    private readonly SessionSettings _settings;
    private readonly IMessageStoreFactory _storeFactory;
    private readonly ILogFactory _logFactory;
    private readonly SocketInitiator _initiator;
    private SessionID? _sessionID;
    private TaskCompletionSource<string>? _responseTcs;

    public FixClient(string configFile)
    {
        _settings = new SessionSettings(configFile);
        var baseDirectory = System.AppDomain.CurrentDomain.BaseDirectory;

        foreach (var sessionID in _settings.GetSessions())
        {
            var sessionSettings = _settings.Get(sessionID);
            
            var dataDictionaryPath = System.IO.Path.Combine(baseDirectory, sessionSettings.GetString("DataDictionary"));
            sessionSettings.SetString("DataDictionary", dataDictionaryPath);

            var fileStorePath = System.IO.Path.Combine(baseDirectory, sessionSettings.GetString("FileStorePath"));
            sessionSettings.SetString("FileStorePath", fileStorePath);

            var fileLogPath = System.IO.Path.Combine(baseDirectory, sessionSettings.GetString("FileLogPath"));
            sessionSettings.SetString("FileLogPath", fileLogPath);
        }
        
        _storeFactory = new FileStoreFactory(_settings);
        _logFactory = new FileLogFactory(_settings);
        _initiator = new SocketInitiator(this, _storeFactory, _settings, _logFactory);
    }

    public void Start()
    {
        _initiator.Start();
    }

    public void Stop()
    {
        _initiator.Stop();
    }

    public Task<string> SendOrderAsync(string symbol, char side, int quantity, decimal price)
    {
        if (_sessionID == null)
        {
            Console.WriteLine("Não foi possível enviar a ordem: Sessão não foi criada.");
            return Task.FromResult("Erro: A sessão FIX não foi criada. Verifique o console do servidor.");
        }

        Console.WriteLine("Enviando nova ordem...");
        var newOrderSingle = new QuickFix.FIX44.NewOrderSingle(
            new ClOrdID(Guid.NewGuid().ToString()),
            new Symbol(symbol),
            new Side(side),
            new TransactTime(DateTime.UtcNow),
            new OrdType(OrdType.LIMIT)
        );

        newOrderSingle.Set(new OrderQty(quantity));
        newOrderSingle.Set(new Price(price));

        _responseTcs = new TaskCompletionSource<string>();
        Session.SendToTarget(newOrderSingle, _sessionID);
        return _responseTcs.Task;
    }

    public void FromAdmin(Message message, SessionID sessionID) { }
    public void ToAdmin(Message message, SessionID sessionID) { }
    public void FromApp(Message message, SessionID sessionID)
    {
        try
        {
            Crack(message, sessionID);
        }
        catch (Exception ex)
        {
            _responseTcs?.TrySetResult($"Erro ao processar resposta: {ex.Message}");
        }
        _sessionID = sessionID;
    }
    public void ToApp(Message message, SessionID sessionID) { }
    public void OnCreate(SessionID sessionID)
    {
        Console.WriteLine($"Sessão criada: {sessionID}");
        _sessionID = sessionID;
    }
    public void OnLogon(SessionID sessionID) 
    {
        Console.WriteLine($"Logon bem-sucedido: {sessionID}");
    }
    public void OnLogout(SessionID sessionID) 
    {
        Console.WriteLine($"Logout: {sessionID}");
        _sessionID = null;
    }

    public void OnMessage(QuickFix.FIX44.ExecutionReport report, SessionID sessionID)
    {
        Console.WriteLine("Relatório de execução recebido.");
        var status = report.OrdStatus.Value;
        if (status == OrdStatus.REJECTED)
        {
            _responseTcs?.TrySetResult($"Ordem Rejeitada: {report.Text.Value}");
        }
        else
        {
            _responseTcs?.TrySetResult($"Ordem Aceita. ID da Ordem: {report.OrderID.Value}");
        }
    }
} 