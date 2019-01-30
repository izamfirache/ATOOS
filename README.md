# ATOOS
Automated Testing for Object Oriented Software (C#).

The main purpose of this project is to automate the unit test generation process as much as possible. This tool is a Visual Studio Extension which can be used by a developer to generate unit tests automatically for C# code. 

General targeted problem: usually the development/management teams skip writing unit tests because it takes too much time and very often the teams prefer to spend time for developing new features rather than writing unit tests. Using this tool a development team can automatically generate unit tests without writing 70% of the code manually. The tool is integrated in Visual Studio as an extension, so it works for any project developed in C# with VS.

Implementation details:
  - The developer can select the project or the classes for which the tests will be generated and for each method in a class, a unit test will be generated with some assertions.
  - The tool resolve the mocking part automatically, even complex types, based on the dependencies in the targeted class's constructor.
  - Initially all the generated tests will be failing but it will leave a placeholder in the asserts for the developer to put his desired expected result.
  - The tool automatically creates a new test project/class, creates the required mocks, creates the target object, calls the method under test, resolves all the dependencies, creates assertion which makes sure that no exceptions are thrown during the execution, creates an assertion which will compare the result of the method with the expected value (which will be specified by the developer).
 
Used technologies:
  - Reflection for getting metadata about the code
  - Roslyn to inspect (get the body of the methods for ex, thing I can't do with reflection) the solution and the projects under it
  - CodeDOM library to generate the unit test code 
      - this can be done with Roslyn also, but working with the syntax tree is computationally heavy, using CodeDOM for code generation is faster.
  - EnvDTE library to get the projects/classes in the solution and to create new projects/classes under the current solution.
  
The UnitTestExtension project is the startup project, the functionality is wrapped in a VS extension.

Steps to Run a Demo of this functionality:
  1. Set the UnitTestExtension as the StartupProject.
  2. [[If you want to run this tool on your own source code project, skip this step]] In the solution, under the TestSourceProjects folder, move the ATOOSTestSourceCodeProject1 project in a new solution and save it locally on the disk.
  3. Start the ATOOS solution (with the UnitTestExtension as the StartupProject).
  4. In the new VS experimental instance that opened, open the source code project (your own or the test project configured at step 2, you need to select the .sln file).
  5. When your solution is started (in the experimental VS instance), select the source code project (click on it), then go to View --> 
Other Windows --> TestGeneratorManager, if you click on it the ATOOS VS extension window will open.
  6. In the ATOOS VS extension window, select the projects/classes for which you want the unit tests to be generated.
  7. Press the 'Generate unit tests' button (make sure the source code project is selected at this step).
  8. [[Probably]] after a couple of seconds VS will ask you to reload the solution, reload it.
    - this happens because a new project was added to the solution in the process, the unit test project.
  9. Inspect the unit generated unit tests, run them from the Test Explorer window.
