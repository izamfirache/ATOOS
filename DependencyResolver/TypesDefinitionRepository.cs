using ATOOS.Core.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependencyResolver
{
    public class TypesDefinitionRepository
    {
        private SqlConnection _sqlConnection;
        private string ConnectionString = "";
        public TypesDefinitionRepository()
        {
            _sqlConnection = new SqlConnection(ConnectionString);
        }

        public Class GetTypeDefinition(string typeID)
        {
            throw new NotImplementedException();
        }

        public void AddNewTypeDefinition()
        {
            throw new NotImplementedException();
        }

        public int RemoveTypeDefinition(string typeID)
        {
            throw new NotImplementedException();
        }

        public string UpdateTypeDefinition(string typeID)
        {
            throw new NotImplementedException();
        }
    }
}
