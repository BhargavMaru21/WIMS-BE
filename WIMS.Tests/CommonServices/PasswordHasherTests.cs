namespace WIMS.Tests.CommonServices;

using WIMS.Application.CommonServices;
using WIMS.Application.Validators.Admin;
using Xunit;
using Xunit.Abstractions;

public class PasswordHasherTests 
{
    private readonly PasswordHasher _passwordHasher;
    private readonly ITestOutputHelper _output;

    public PasswordHasherTests(ITestOutputHelper output)
    {
        _passwordHasher = new PasswordHasher();
        _output = output;
    }

    [Fact]
    public void Hash_GivenPassword_ReturnsHash()
    {
        string password = "Test@123";

        string passwordHash = _passwordHasher.Hash(password);

        Assert.NotEqual(password, passwordHash);
    }

    [Fact]
    public void Hash_SamePasswordCallTwice_ReturnDifferentHash()
    {
        string password = "Test@123";

        string passwordHash1 = _passwordHasher.Hash(password);
        string passwordHash2 = _passwordHasher.Hash(password);

        Assert.NotEqual(passwordHash1,passwordHash2);
    }

    
    [Fact]
    public void Verify_GivenCorrectPassword_ReturnsTrue()
    {
       
        string password = "Test@123";
        string passwordHash = _passwordHasher.Hash(password);
        
        bool isValid = _passwordHasher.Verify(password, passwordHash);

        Assert.True(isValid);
    }

    [Fact]
    public void Verify_GivenWrongPassword_ReturnsFalse()
    {
        string correctPassword = "Test@123";
        string wrongPassword = "Test";

        string correctPasswordHash = _passwordHasher.Hash(correctPassword);

        bool isValid = _passwordHasher.Verify(wrongPassword, correctPasswordHash);

        Assert.False(isValid);
    }

    [Fact]
    public void NormalHash_GivenInput_ReturnHash()
    {
        string input = "Test";

        string hash = _passwordHasher.NormalHash(input);

        Assert.NotNull(hash);
        Assert.NotEqual(input,hash);
    }

    [Theory]
    [InlineData("Test")]
    [InlineData("Token Hash")]
    [InlineData("")]
    public void NormalHash_CalledTwiceWithSameInput_AlwaysProducesSameHash(string input)
    {
        string hash1 = _passwordHasher.NormalHash(input);
        string hash2 = _passwordHasher.NormalHash(input);

        if(input.Equals("")){
        _output.WriteLine(hash1);
        _output.WriteLine(hash2);
        }

        Assert.Equal(hash1, hash2);
    }
}