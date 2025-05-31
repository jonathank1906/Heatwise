using Xunit;
using Moq;
using Sem2Proj.ViewModels;
using System;

namespace Sem2Proj.Tests;

public class LoginViewModelTests
{
    [Fact]
    public void AttemptLogin_WithValidCredentials_ShouldInvokeSuccess()
    {
        // Arrange
        var viewModel = new LoginViewModel();
        bool successInvoked = false;
        viewModel.Success += () => successInvoked = true;

        // Act
        viewModel.Username = "admin";
        viewModel.Password = "admin";
        viewModel.AttemptLoginCommand.Execute(null);

        // Assert
        Assert.True(successInvoked);
        Assert.Equal("", viewModel.ErrorMessage);
    }

    [Fact]
    public void AttemptLogin_WithInvalidCredentials_ShouldShowErrorMessage()
    {
        // Tests that wrong credentials show error message
        // Arrange
        var viewModel = new LoginViewModel();
        bool successInvoked = false;
        viewModel.Success += () => successInvoked = true;

        // Act
        viewModel.Username = "wrong";
        viewModel.Password = "wrong";
        viewModel.AttemptLoginCommand.Execute(null);

        // Assert
        Assert.False(successInvoked);
        Assert.Equal("Invalid username or password.", viewModel.ErrorMessage);
    }

    [Fact]
    public void AttemptLogin_WithEmptyCredentials_ShouldShowErrorMessage()
    {
        // Arrange
        var viewModel = new LoginViewModel();
        bool successInvoked = false;
        viewModel.Success += () => successInvoked = true;

        // Act
        viewModel.Username = "";
        viewModel.Password = "";
        viewModel.AttemptLoginCommand.Execute(null);

        // Assert
        Assert.False(successInvoked);
        Assert.Equal("Invalid username or password.", viewModel.ErrorMessage);
    }

    [Fact]
    public void DummyTest_LoginViewModel()
    {
        Assert.True(true);
    }
} 