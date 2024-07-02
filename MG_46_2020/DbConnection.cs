using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG_46_2020
{
    public class DbConnection
    {
        private static SqlConnection _conn = null;

        public static SqlConnection GetConnection
        {
            get
            {
                if (_conn == null)
                    _conn = new SqlConnection(@"Data Source=(localdb)\mgProjekat; Initial Catalog = MemoryGame; Integrated Security = True");
                return _conn;
            }
        }

    }
}
