using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATOOSTestSourceCodeProject1
{
    public class Employee : IEmployee
    {
        private string Name;
        private string Surname;
        private int Age;

        public Employee(string name, string surname, int age)
        {
            Name = name;
            Surname = surname;
            Age = age;
        }

        public string GetEmployeeInfo(string name, string surname, int age)
        {
            return string.Format("{0} {1}, {2} years of age.", name, surname, age);
        }

        public string GetEmployeeName()
        {
            //return Name;
            return "Not even going there";
        }

        public string GetEmployeeSurname()
        {
            //return Surname;
            return "Not even going there";
        }

        public int GetEmployeeAge()
        {
            throw new Exception("no reason");
            //return Age;
            return 11;
        }

        public int GetEmployeeSalary(Employee employee)
        {
            if (employee.Age > 10)
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
