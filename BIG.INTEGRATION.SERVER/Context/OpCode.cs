using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIG.INTEGRATION.SERVER.Context
{   
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

        FO_ORDER,
        FO_TRADE,
        FO_CASHWITHDRAW,
        FO_STOCKWITHDRAW,
        FO_EXERCISE,
        FO_REKSADANA,
    }
    
}
