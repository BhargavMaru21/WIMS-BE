using System.Linq.Expressions;
using AutoMapper;
using Moq;
using WIMS.Application.DTOs.UnitOfMeasure;
using WIMS.Application.Interfaces.Common;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Application.Service.UnitOfMeasure;
using WIMS.Domain.Entity;
using Xunit;
using Xunit.Abstractions;

namespace WIMS.Tests.Services;

public class UnitOfMeasureServiceTests
{
    private readonly Mock<IUnitOfMeasureRepository> _mockRepo;
    private readonly Mock<IInputNormalizer> _mockInputNormalizer;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly UnitOfMeasureService _service;
    private readonly ITestOutputHelper _output;

    public UnitOfMeasureServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _mockRepo = new Mock<IUnitOfMeasureRepository>();
        _mockInputNormalizer = new Mock<IInputNormalizer>();
        _mockMapper = new Mock<IMapper>();
        _mockCurrentUser = new Mock<ICurrentUserService>();

        _service = new UnitOfMeasureService(
            _mockRepo.Object,
            _mockInputNormalizer.Object,
            _mockMapper.Object,
            _mockCurrentUser.Object);
    }



    //create

    [Fact]
    public async Task CreateUnit_GivenValidInput_ReturnsNewUnit()
    {
        var request = new CreateUnitRequest { Name = "Testing", Abbreviation = "tt" };
        var mappedRequest = new UnitsOfMeasure { Name = "Testing", Abbreviation = "tt" };
        var savedEntity = new UnitsOfMeasure { Id = 10, Name = "Testing", Abbreviation = "tt" };
        var expectedResponse = new UnitResponse { Id = 10, Name = "Testing", Abbreviation = "tt" };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);

        _mockMapper
             .Setup(x => x.Map<UnitsOfMeasure>(request))
             .Returns(mappedRequest);

        _mockRepo
            .Setup(x => x.CreateAsync(mappedRequest))
            .ReturnsAsync(savedEntity);

        _mockMapper
            .Setup(x => x.Map<UnitResponse>(savedEntity))
            .Returns(expectedResponse);

        var result = await _service.CreateUnit(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal("Testing", result.Data!.Name);

    }


    [Fact]
    public async Task CreateUnit_GivenDuplicateName_ReturnsFailure()
    {
        var request = new CreateUnitRequest { Name = "Testing", Abbreviation = "tt" };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);

        _mockRepo
            .Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<UnitsOfMeasure, bool>>>()))
            .ReturnsAsync(true);

        var result = await _service.CreateUnit(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Unit name already exists.", result.Message);

        _mockRepo.Verify(x => x.CreateAsync(It.IsAny<UnitsOfMeasure>()), Times.Never);
    }

    [Fact]
    public async Task CreateUnit_GivenDuplicateAbbreviation_ReturnsFailure()
    {

        var request = new CreateUnitRequest { Name = "Kilogram", Abbreviation = "kg" };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);

        _mockRepo
            .SetupSequence(x => x.ExistsAsync(It.IsAny<Expression<Func<UnitsOfMeasure, bool>>>()))
            .ReturnsAsync(false)
            .ReturnsAsync(true);

        var result = await _service.CreateUnit(request);

        _output.WriteLine(result.Message);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Unit Abbreviation already exists.", result.Message);
    }



    //update
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateUnit_GivenInvalidId_ReturnsFailure(int invalidId)
    {
        var result = await _service.UpdateUnit(invalidId, new UpdateUnitRequest { Name = "Test" });

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Invalid ID", result.Message);
    }

    [Fact]
    public async Task UpdateUnit_GivenUnitDoesNotExist_ReturnsFailure()
    {
        var request = new UpdateUnitRequest { Name = "Test" };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);

        _mockRepo.Setup(x => x.GetAsync(It.IsAny<Expression<Func<UnitsOfMeasure, bool>>>(),
                                        It.IsAny<bool>(),
                                        It.IsAny<Func<IQueryable<UnitsOfMeasure>, IQueryable<UnitsOfMeasure>>>())).ReturnsAsync((UnitsOfMeasure?)null);

        var result = await _service.UpdateUnit(99, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }


    [Fact]
    public async Task UpdateUnit_GivenDuplicateUnitName_ReturnFailure()
    {
        var request = new UpdateUnitRequest { Name = "Test" };
        var existingUnit = new UnitsOfMeasure { Id = 20, Name = "Test", Abbreviation = "tt" };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);

        _mockRepo
            .Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<UnitsOfMeasure, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Func<IQueryable<UnitsOfMeasure>, IQueryable<UnitsOfMeasure>>?>()))
            .ReturnsAsync(existingUnit);

        _mockRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<UnitsOfMeasure, bool>>>())).ReturnsAsync(true);

        var result = await _service.UpdateUnit(20, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("A unit of measure with this name already exists.", result.Message);
    }


    [Fact]
    public async Task UpdateUnit_GivenDuplicateAbbreviationName_ReturnFailure()
    {
        var request = new UpdateUnitRequest { Abbreviation = "tt" };
        var existingUnit = new UnitsOfMeasure { Id = 20, Name = "Test", Abbreviation = "tt" };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);

        _mockRepo
            .Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<UnitsOfMeasure, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Func<IQueryable<UnitsOfMeasure>, IQueryable<UnitsOfMeasure>>?>()))
            .ReturnsAsync(existingUnit);

        _mockRepo
            .Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<UnitsOfMeasure, bool>>>()))
            .ReturnsAsync(true);

        var result = await _service.UpdateUnit(20, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("A unit of measure with this abbreviation already exists.", result.Message);
    }


    [Fact]
    public async Task UpdateUnit_GivenPartialValidChanges_UpdatesAndReturnsSuccess()
    {
        var existingUnit = new UnitsOfMeasure { Id = 5, Name = "Kilogram", Abbreviation = "kg" };
        var request = new UpdateUnitRequest { Name = "Test" };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);

        _mockRepo
            .Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<UnitsOfMeasure, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Func<IQueryable<UnitsOfMeasure>, IQueryable<UnitsOfMeasure>>?>()))
            .ReturnsAsync(existingUnit);

        _mockRepo
            .Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<UnitsOfMeasure, bool>>>()))
            .ReturnsAsync(false);

        _mockRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        _mockMapper
            .Setup(x => x.Map<UnitResponse>(existingUnit))
            .Returns(new UnitResponse { Id = 5, Name = "Test", Abbreviation = "kg" });

        var result = await _service.UpdateUnit(5, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("Test", result.Data!.Name);
        Assert.Equal("kg", result.Data.Abbreviation);
        _mockRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }



    [Fact]
    public async Task UpdateUnit_GivenFullValidChanges_UpdatesAndReturnsSuccess()
    {
        var existingUnit = new UnitsOfMeasure { Id = 5, Name = "Kilogram", Abbreviation = "kg" };
        var request = new UpdateUnitRequest { Name = "Test", Abbreviation = "tt" };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);

        _mockRepo
            .Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<UnitsOfMeasure, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Func<IQueryable<UnitsOfMeasure>, IQueryable<UnitsOfMeasure>>?>()))
            .ReturnsAsync(existingUnit);

        _mockRepo
            .Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<UnitsOfMeasure, bool>>>()))
            .ReturnsAsync(false);

        _mockRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        _mockMapper
            .Setup(x => x.Map<UnitResponse>(existingUnit))
            .Returns(new UnitResponse { Id = 5, Name = "Test", Abbreviation = "tt" });

        var result = await _service.UpdateUnit(5, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("Test", result.Data!.Name);
        Assert.Equal("tt", result.Data.Abbreviation);
        _mockRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }


    //delete
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteUnit_GivenInvalidId_ReturnsFailure(int invalidId)
    {
        var result = await _service.DeleteUnit(invalidId);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Invalid ID", result.Message);
    }


    [Fact]
    public async Task DeleteUnit_GivenUnitDoesNotExist_ReturnsFailure()
    {
        _mockRepo
            .Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<UnitsOfMeasure, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Func<IQueryable<UnitsOfMeasure>, IQueryable<UnitsOfMeasure>>?>()))
            .ReturnsAsync((UnitsOfMeasure?)null);

        var result = await _service.DeleteUnit(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Unit not Found",result.Message);
    }

    [Fact]
    public async Task DeleteUnit_GivenUnitIsAssignedToProduct_ReturnsFailure()
    {
        var existingUnit = new UnitsOfMeasure { Id = 5, Name = "Test", Abbreviation = "tt" };

        _mockRepo
            .Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<UnitsOfMeasure, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Func<IQueryable<UnitsOfMeasure>, IQueryable<UnitsOfMeasure>>?>()))
            .ReturnsAsync(existingUnit);

        _mockRepo.Setup(x => x.IsAssignedToProductAsync(5)).ReturnsAsync(true);

        var result = await _service.DeleteUnit(5);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task DeleteUnit_GivenUnitCanBeDeleted_ReturnsSuccess()
    {
        var existingUnit = new UnitsOfMeasure { Id = 5, Name = "Test", Abbreviation = "tt" };

        _mockRepo
            .Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<UnitsOfMeasure, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Func<IQueryable<UnitsOfMeasure>, IQueryable<UnitsOfMeasure>>?>()))
            .ReturnsAsync(existingUnit);

        _mockRepo.Setup(x => x.IsAssignedToProductAsync(5)).ReturnsAsync(false);
        _mockRepo.Setup(x => x.DeleteAsync(existingUnit)).ReturnsAsync(true);

        var result = await _service.DeleteUnit(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
    }

}