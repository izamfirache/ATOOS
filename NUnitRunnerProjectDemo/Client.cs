using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnitRunnerProjectDemo
{
    public class Client
    {
        public string Name;
        public string Surname;
        public int Age;
        public IEmployee AssociatedEmployee;

        public Client(string name, string surname, int age, IEmployee associatedEmployee)
        {
            Name = name;
            Surname = surname;
            Age = age;
            AssociatedEmployee = associatedEmployee;
        }

        public string GetClientInfo(string name, string surname, int age)
        {
            return string.Format("{0} {1}, {2} years of age.", name, surname, age);
        }

        public string GetAssociatedEmployeeInfo()
        {
            return string.Format("{0} {1}, {2} years of age.", 
                AssociatedEmployee.GetEmployeeName(),
                AssociatedEmployee.GetEmployeeSurname(), 
                AssociatedEmployee.GetEmployeeAge());
        }

        public string GetClientName()
        {
            return Name;
        }

        public string GetClientSurname()
        {
            return Surname;
        }

        public int GetClientSalary(Client client)
        {
            if (client.Age > 10)
            {
                return 1000;
            }
            else
            {
                return 2000;
            }
        }

        public string ReturnNullForTestingPurpose()
        {
            return null;
        }
    }
}
