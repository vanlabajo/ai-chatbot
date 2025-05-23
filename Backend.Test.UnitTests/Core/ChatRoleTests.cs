using Backend.Core.Models;

namespace Backend.Test.UnitTests.Core
{
    public class ChatRoleTests
    {
        [Fact]
        public void ChatRole_User_ShouldHaveCorrectValue()
        {
            // Arrange
            var expectedValue = "user";
            // Act
            var role = ChatRole.User;
            // Assert
            Assert.Equal(expectedValue, role.ToString());
        }
        [Fact]
        public void ChatRole_Assistant_ShouldHaveCorrectValue()
        {
            // Arrange
            var expectedValue = "assistant";
            // Act
            var role = ChatRole.Assistant;
            // Assert
            Assert.Equal(expectedValue, role.ToString());
        }
        [Fact]
        public void ChatRole_System_ShouldHaveCorrectValue()
        {
            // Arrange
            var expectedValue = "system";
            // Act
            var role = ChatRole.System;
            // Assert
            Assert.Equal(expectedValue, role.ToString());
        }

        [Fact]
        public void ChatRole_Equals_ShouldReturnTrue_WhenSameRole()
        {
            // Arrange
            var role1 = ChatRole.User;
            var role2 = ChatRole.User;
            // Act
            var result = role1.Equals(role2);
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ChatRole_Equals_ShouldReturnFalse_WhenDifferentRoles()
        {
            // Arrange
            var role1 = ChatRole.User;
            var role2 = ChatRole.Assistant;
            // Act
            var result = role1.Equals(role2);
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ChatRole_GetHashCode_ShouldReturnSameValue_ForSameRole()
        {
            // Arrange
            var role1 = ChatRole.User;
            var role2 = ChatRole.User;
            // Act
            var hash1 = role1.GetHashCode();
            var hash2 = role2.GetHashCode();
            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void ChatRole_NotEquals_ShouldReturnTrue_WhenDifferentRoles()
        {
            // Arrange
            var role1 = ChatRole.User;
            var role2 = ChatRole.Assistant;
            // Act
            var result = role1 != role2;
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ChatRole_EqualsOperator_ShouldReturnTrue_WhenSameRole()
        {
            // Arrange
            var role1 = ChatRole.User;
            var role2 = ChatRole.User;
            // Act
            var result = role1 == role2;
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ChatRole_Equals_ShouldReturnTrue_WhenSameObject()
        {
            // Arrange
            var role1 = ChatRole.User;
            var role2 = ChatRole.User;
            // Act
            var result = role1.Equals((object)role2);
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ChatRole_WithNull_Object_ShouldReturnFalse()
        {
            // Arrange
            var role = ChatRole.User;
            // Act
            var result = role.Equals(null);
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ChatRole_WithDifferentType_ShouldReturnFalse()
        {
            // Arrange
            var role = ChatRole.User;
            var differentType = new object();
            // Act
            var result = role.Equals(differentType);
            // Assert
            Assert.False(result);
        }
    }
}
