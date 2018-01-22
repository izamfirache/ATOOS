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
            // IEmployee represents an external dependency for the Class Under Test (CUT)
            _employeeMock = new Mock<IEmployee>();

            // these methods actually go into the database and get the name, surname, age ...
            // we want to mock them in order to isolate and test only the CUT
            _employeeMock.Setup(m => m.GetEmployeeName()).Returns("MockedName");
            _employeeMock.Setup(m => m.GetEmployeeSurname()).Returns("MockedSurname");
            _employeeMock.Setup(m => m.GetEmployeeAge()).Returns(99);
            _employeeMock.Setup(m => m.GetEmployeeInfo(It.IsAny<String>(), It.IsAny<String>(), 
                It.IsAny<Int32>())).Returns("");
        }

        [TestCase()]
        public virtual void GetAssociatedEmployeeInfo_RandomInput_FillExpectedResult()
        {
            var client = new Client("clientName", "clientSurname", 50, _employeeMock.Object);
            var result = client.GetAssociatedEmployeeInfo();

            Assert.AreEqual(result, "Placeholder, the developer will insert the expected value here.");
        }

        [TestCase()]
        public virtual void GetAssociatedEmployeeInfo_RandomInput_ShouldNotThrowException()
        {
            var client = new Client("clientName", "clientSurname", 50, _employeeMock.Object);

            // method execution should not throw any exception (error)
            Assert.DoesNotThrow(() => client.GetAssociatedEmployeeInfo());
        }

        [TestCase()]
        public virtual void GetAssociatedEmployeeInfo_RandomInput_ShouldCallMockedMethod()
        {
            var client = new Client("clientName", "clientSurname", 50, _employeeMock.Object);

            // - make sure that the mocked methods are indeed called
            // - this kind of tests are usefull to make sure that the mocked methods are called 
            // with the correct mocked parameters
            // - also such a test can be used as a milestone in the code,
            // for example if a method invocation throws an exeption, the question is
            // which line threw that exception, and having a test like this can tell you
            // that the exception was for sure thrown after the invocation of that mocked function

            _employeeMock.Verify(m => m.GetEmployeeName(), Times.Once());
            _employeeMock.Verify(m => m.GetEmployeeSurname(), Times.Once());
            _employeeMock.Verify(m => m.GetEmployeeAge(), Times.Once());
        }
    }
}
