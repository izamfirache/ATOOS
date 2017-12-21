using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnitRunnerProjectDemo
{
    public interface IEmployee
    {
        string GetEmployeeInfo(string name, string surname, int age);

        string GetEmployeeName();

        string GetEmployeeSurname();
        int GetEmployeeAge();

        int GetEmployeeSalary(Employee employee);

        string ReturnNullForTestingPurpose();
    }
}
