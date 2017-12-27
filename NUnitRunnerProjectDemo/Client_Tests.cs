using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NUnitRunnerProjectDemo
{
    [TestFixture]
    public class Client_Tests
    {
        private Mock<IEmployee> _employeeMock;
        public Client_Tests()
        {
            _employeeMock = new Mock<IEmployee>();

            //_employeeMock.Setup(m => m.GetEmployeeName()).Returns("MockedName");
            _employeeMock.Setup(m => m.GetEmployeeSurname()).Returns("MockedSurname");
            _employeeMock.Setup(m => m.GetEmployeeAge()).Returns(99);
            _employeeMock.Setup(m => m.GetEmployeeInfo(It.IsAny<String>(), It.IsAny<String>(), 
                It.IsAny<Int32>())).Returns("");

            // dynamically generate the code for a lamda expression
            var parameter = Expression.Parameter(typeof(IEmployee), "m");
            MethodInfo methodInfo = typeof(IEmployee).GetMethod("GetEmployeeName");
            MethodCallExpression methodCall = Expression.Call(parameter, methodInfo); // m.GetEmployeeName()
            var getEmployeeNameLambdaExpr = 
                Expression.Lambda<Func<IEmployee, string>>(methodCall, parameter); // m => m.GetEmployeeName()

            var x = _employeeMock.Setup(getEmployeeNameLambdaExpr);
            x.Returns("MockedName");
        }

        [TestCase()]
        public virtual void MockExternalMethodTest()
        {
            var client = new Client("clientName", "clientSurname", 50, _employeeMock.Object);
            var result = client.GetAssociatedEmployeeInfo();
            Assert.AreEqual(result, "MockedName MockedSurname, 99 years of age.");
        }
    }
}
