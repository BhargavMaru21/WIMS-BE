using WIMS.Application.Common.Services;
using Xunit.Abstractions;

namespace WIMS.Tests.CommonServices;

public class InputNormalizerTests
{
    private readonly InputNormalizer _inputNormalizer;
    private readonly ITestOutputHelper _output;

    public InputNormalizerTests(ITestOutputHelper output)
    {
        _output = output; 
        _inputNormalizer = new InputNormalizer();
    }

    [Fact]
    public void Normalize_GivenText_ReturnsTrimedValue()
    {
        string input = "   test   ";

        string trimedText = _inputNormalizer.Normalize(input);

        _output.WriteLine(trimedText);

        Assert.Equal("test",trimedText);
    }

    [Fact]
    public void NormalizeEmail_GivenEmail_ReturnsNormalizeEmail()
    {
        string input = "  Bhargav@gmail.com   ";

        string normalizeEmail = _inputNormalizer.NormalizeEmail(input);

        _output.WriteLine(normalizeEmail);

        Assert.Equal("bhargav@gmail.com",normalizeEmail);
    }

    [Fact]
    public void NormalizeObject_GivenObject_ReturnsNormalizeObject()
    {
        Helper obj = new Helper {
            Id = 1,
            Name = "    Test    ",
            Email = "    TEST@gmail.com    "
        };

         Helper expectedObj = new Helper {
            Id = 1,
            Name = "Test",
            Email = "test@gmail.com"
        };

        obj = _inputNormalizer.NormalizeObject(obj);

        _output.WriteLine(obj.Name);
        _output.WriteLine(obj.Email);

        Assert.Equal(expectedObj.Name,obj.Name);
        Assert.Equal(expectedObj.Email,obj.Email);
    }
}

public class Helper {
    public int Id {get; set;}
    public required string Name {get; set;}
    public required string Email {get; set;}
}
