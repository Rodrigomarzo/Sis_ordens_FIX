using QuickFix;
using QuickFix.Fields;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;
using System.Collections.Concurrent;

public class FixServer : QuickFix.MessageCracker, IApplication
{
    private readonly SessionSettings _settings;
    private readonly IMessageStoreFactory _storeFactory;
    private readonly ILogFactory _logFactory;
    private readonly ThreadedSocketAcceptor _acceptor;
    private readonly ConcurrentDictionary<string, decimal> _exposures = new();
    private const decimal ExposureLimit = 100_000_000;

    public FixServer(string configFile)
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
        _acceptor = new ThreadedSocketAcceptor(this, _storeFactory, _settings, _logFactory);
    }

    public void Start()
    {
        _acceptor.Start();
    }

    public void Stop()
    {
        _acceptor.Stop();
    }

    public void FromAdmin(Message message, SessionID sessionID) { }

    public void ToAdmin(Message message, SessionID sessionID) { }

    public void FromApp(Message message, SessionID sessionID)
    {
        Crack(message, sessionID);
    }

    public void ToApp(Message message, SessionID sessionID) { }

    public void OnCreate(SessionID sessionID) { }

    public void OnLogon(SessionID sessionID) { }

    public void OnLogout(SessionID sessionID) { }

    public void OnMessage(QuickFix.FIX44.NewOrderSingle order, SessionID sessionID)
    {
        var symbol = order.Symbol.Value;
        var side = order.Side.Value;
        var orderQty = order.OrderQty.Value;
        var price = order.Price.Value;
        var clOrdID = order.ClOrdID.Value;

        var orderFinancialValue = price * orderQty;
        var currentExposure = _exposures.GetOrAdd(symbol, 0);

        decimal potentialNewExposure;
        if (side == Side.BUY)
        {
            potentialNewExposure = currentExposure + orderFinancialValue;
        }
        else // Side == SELL
        {
            potentialNewExposure = currentExposure - orderFinancialValue;
        }

        if (Math.Abs(potentialNewExposure) > ExposureLimit)
        {
            Console.WriteLine($"Ordem REJEITADA. Símbolo: {symbol}, Valor: {orderFinancialValue:C}. Excederia o limite. Exposição Atual: {currentExposure:C}");
            RejectOrder(sessionID, order, $"O limite de exposição para o símbolo {symbol} seria excedido.");
        }
        else
        {
            _exposures[symbol] = potentialNewExposure;
            Console.WriteLine($"Ordem ACEITA. Símbolo: {symbol}, Nova Exposição: {potentialNewExposure:C}");
            AcceptOrder(sessionID, clOrdID, symbol, side, orderQty, price);
        }
    }

    private void AcceptOrder(SessionID sessionID, string clOrdID, string symbol, char side, decimal orderQty, decimal price)
    {
        var execReport = new QuickFix.FIX44.ExecutionReport(
            new OrderID(Guid.NewGuid().ToString()),
            new ExecID(Guid.NewGuid().ToString()),
            new ExecType(ExecType.NEW),
            new OrdStatus(OrdStatus.NEW),
            new Symbol(symbol),
            new Side(side),
            new LeavesQty(orderQty),
            new CumQty(0),
            new AvgPx(0)
        );

        execReport.Set(new ClOrdID(clOrdID));
        execReport.Set(new OrderQty(orderQty));
        execReport.Set(new Price(price));

        try
        {
            Session.SendToTarget(execReport, sessionID);
        }
        catch (SessionNotFound ex)
        {
            Console.WriteLine($"Erro ao enviar mensagem: {ex.Message}");
        }
    }

    private void RejectOrder(SessionID sessionID, QuickFix.FIX44.NewOrderSingle order, string reason)
    {
        var execReport = new QuickFix.FIX44.ExecutionReport(
            new OrderID(Guid.NewGuid().ToString()),
            new ExecID(Guid.NewGuid().ToString()),
            new ExecType(ExecType.REJECTED),
            new OrdStatus(OrdStatus.REJECTED),
            order.Symbol,
            order.Side,
            new LeavesQty(order.OrderQty.Value),
            new CumQty(0),
            new AvgPx(0)
        );

        execReport.Set(order.ClOrdID);
        execReport.Set(new Text(reason));
        
        try
        {
            Session.SendToTarget(execReport, sessionID);
        }
        catch (SessionNotFound ex)
        {
            Console.WriteLine($"Erro ao enviar mensagem: {ex.Message}");
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        var baseDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
        var configFile = System.IO.Path.Combine(baseDirectory, "quickfix-server.cfg");

        var server = new FixServer(configFile);
        server.Start();
        Console.WriteLine("Pressione qualquer tecla para sair.");
        Console.ReadKey();
        server.Stop();
    }
}
