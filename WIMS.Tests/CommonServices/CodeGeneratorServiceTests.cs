using WIMS.Application.CommonServices;
using Xunit.Abstractions;

namespace WIMS.Tests.CommonServices;

public class CodeGeneratorServiceTests
{
    private readonly CodeGeneratorService _codeGeneratorService;
    private readonly ITestOutputHelper _output;

    public CodeGeneratorServiceTests(ITestOutputHelper output)
    {
        _codeGeneratorService = new CodeGeneratorService();
        _output = output;
    }

    [Fact]
    public void GenerateCode_GivenValidInput_ReturnCode()
    {
        string entityName = "warehouse";
        int number = 1;

        string code = _codeGeneratorService.GenerateCode(entityName,number);

        Assert.Equal("WH-001",code);
    }

    [Fact]
    public void GenerateCode_GivenWrongEntityName_ReturnFalse()
    {
        string entityName = "Test";
        int number = 1;

        var exception = Assert.Throws<ArgumentException>(()=> _codeGeneratorService.GenerateCode(entityName,number));

        _output.WriteLine(exception.Message);

        Assert.Equal("Invalid entity name",exception.Message);
    }
}
