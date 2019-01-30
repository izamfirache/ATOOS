namespace ATOOSTestSourceCodeProject1
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