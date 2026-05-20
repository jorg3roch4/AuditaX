using System.Collections.Generic;
using AuditaX.Configuration;
using Microsoft.Extensions.Configuration;

namespace AuditaX.Tests.Configuration;

public class AuditaXOptionsJsonBindingTests
{
    [Fact]
    public void JsonConfig_Binds_Identifier_Property()
    {
        // Arrange — simulate appsettings.json with both Key and Identifier
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["AuditaX:Entities:FakeUser:Key"] = "Id",
            ["AuditaX:Entities:FakeUser:Identifier"] = "UserName"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var options = new AuditaXOptions();
        configuration.GetSection("AuditaX").Bind(options);

        // Act
        var entity = options.GetEntity(typeof(FakeUser));

        // Assert
        entity.Should().NotBeNull();
        entity!.Key.Should().Be("Id");
        entity.Identifier.Should().Be("UserName");
        entity.KeySelector.Should().NotBeNull();
        entity.IdentifierSelector.Should().NotBeNull();

        var user = new FakeUser { Id = 7, UserName = "alice" };
        entity.KeySelector!(user).Should().Be("7");
        entity.IdentifierSelector!(user).Should().Be("alice");
    }

    [Fact]
    public void JsonConfig_Without_Identifier_Yields_Null_Selector_With_Fallback_To_Key()
    {
        // Arrange — only Key, no Identifier
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["AuditaX:Entities:FakeUser:Key"] = "Id"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var options = new AuditaXOptions();
        configuration.GetSection("AuditaX").Bind(options);

        // Act
        var entity = options.GetEntity(typeof(FakeUser));

        // Assert
        entity.Should().NotBeNull();
        entity!.IdentifierSelector.Should().BeNull();

        var user = new FakeUser { Id = 7, UserName = "alice" };
        entity.GetIdentifier(user).Should().Be(entity.GetKey(user));
        entity.GetIdentifier(user).Should().Be("7");
    }

    private sealed class FakeUser
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
    }
}
