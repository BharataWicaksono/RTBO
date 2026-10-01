using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIG.INTEGRATION.SERVER.Context
{
    public enum HandleInst
    {
        NormalOrder = 0,
        DMALowTouch = 1,
        DMAHighTouch = 2,
        Cared = 3,
        ManualExecution = 4,
        MarketExecution = 5,
        MergedOrder = 6,
        CashWithdrawal = 9,
        StockWithdrawal = 10,
        Exercise = 11,
        SBR = 20,
        MutualFund = 21,
        Bond = 22,
        Reksadana = 23,
        //ORI = 23,
        SUKRI = 24,
        OrderWithAlgo = 40,
        AlgoExecution = 41,
    }

    public enum ServerID
    {
        Exporter = 8,
        Loader = 8,
        RTBO = 21,
    }
    public enum TimestampName
    {
        UNKNOWN = 0,
        BO_BROKER,
        BO_CLIENT,
        BO_CLIENTCASH,
        BO_CLIENTSTOCK,
        BO_OFFICE,
        BO_STOCK,
        BO_USER,
        BO_MARGINALERT,
        BO_HOLIDAY,
        BO_FUNDINOUT,
        BO_STOCKINOUT,
        BO_EXERCISE,
        BO_UNITREGISTRY,
        BO_FUND,
        BO_BANKINSTRUCTION,
        BO_BRIINSTRUCTION,
        BO_FILE_CLIENT,//sample

        FO_ORDER,
        FO_TRADE,
        FO_USERPASSPIN,
        FO_CASHWITHDRAW,
        FO_STOCKWITHDRAW,
        FO_EXERCISE,
        FO_REKSADANA,
    }
    public enum LedgerType
    {
        RTLEDGER_NOTHING,//0
        RTLEDGER_SERVER_START,//1
        RTLEDGER_STOCK,//2
        RTLEDGER_STOCK_CONTROL,//3
        RTLEDGER_STOCK_DATAFEED,//4
        RTLEDGER_BROKER,//5
        RTLEDGER_MARKET,//6
        RTLEDGER_BOARD,//7
        RTLEDGER_CURRENCY,//8
        RTLEDGER_SXCONN,//9
        RTLEDGER_SXSESS,//10
        RTLEDGER_BROKER_INTFC,//11
        RTLEDGER_CLIENT,//12
        RTLEDGER_CLIENT_CONTROL,//13
        RTLEDGER_CLIENT_CASH,//14
        RTLEDGER_CLIENT_STOCK,//15
        RTLEDGER_USER,//16
        RTLEDGER_USER_PERMISSION,//17
        RTLEDGER_USER_CLIENT_MAP,//18
        RTLEDGER_USER_DEALER_CONTROLLER,//19
        RTLEDGER_LOGON,//20
        RTLEDGER_LOGON_ACK,//21
        RTLEDGER_LOGON_NAK,//22
        RTLEDGER_LOGON_ENABLE,//23
        RTLEDGER_CHANGE_PWD,//24
        RTLEDGER_CHANGE_PWD_ACK,//25
        RTLEDGER_CHANGE_PWD_NAK,//26
        RTLEDGER_PWD_POLICY,//27
        RTLEDGER_ORDER_NEW_SAVE,//28
        RTLEDGER_ORDER_NEW_BASKET,//29
        RTLEDGER_ORDER_NEW_SENT,//30
        RTLEDGER_ORDER_NEW_ACK,//31
        RTLEDGER_ORDER_NEW_NAK,//32
        RTLEDGER_ORDER_MATCH,//33
        RTLEDGER_ORDER_AMEND_SAVE,//34
        RTLEDGER_ORDER_AMEND_SENT,//35
        RTLEDGER_ORDER_AMEND_ACK,//36
        RTLEDGER_ORDER_AMEND_NAK,//37
        RTLEDGER_ORDER_WITHDRAW_SAVE,//38
        RTLEDGER_ORDER_WITHDRAW_SENT,//39
        RTLEDGER_ORDER_WITHDRAW_ACK,//40
        RTLEDGER_ORDER_WITHDRAW_NAK,//41
        RTLEDGER_ORDER_REQUEST_APPROVE,//42
        RTLEDGER_ORDER_APPROVED,//43
        RTLEDGER_ORDER_REJECT,//44
        RTLEDGER_RT_ORDER,//45
        RTLEDGER_RT_TRADE,//46
        RTLEDGER_RT_NEGDEAL,//47
        RTLEDGER_USER_DISCONECTED,//48
        RTLEDGER_BROADCAST_INFO,//49
    }
}
