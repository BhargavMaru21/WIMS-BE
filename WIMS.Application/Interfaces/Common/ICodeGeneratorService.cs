namespace WIMS.Application.Interfaces.Common;

public interface ICodeGeneratorService
{
    string GenerateCode(string entityName, int number);
}
