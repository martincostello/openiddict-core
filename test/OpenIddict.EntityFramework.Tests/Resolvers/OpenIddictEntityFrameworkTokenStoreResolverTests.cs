﻿/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System;
using System.Data.Entity;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using OpenIddict.Abstractions;
using OpenIddict.EntityFramework.Models;
using Xunit;
using static OpenIddict.EntityFramework.OpenIddictEntityFrameworkTokenStoreResolver;

namespace OpenIddict.EntityFramework.Tests
{
    public class OpenIddictEntityFrameworkTokenStoreResolverTests
    {
        [Fact]
        public void Get_ReturnsCustomStoreCorrespondingToTheSpecifiedTypeWhenAvailable()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<IOpenIddictTokenStore<CustomToken>>());

            var options = Mock.Of<IOptionsMonitor<OpenIddictEntityFrameworkOptions>>();
            var provider = services.BuildServiceProvider();
            var resolver = new OpenIddictEntityFrameworkTokenStoreResolver(new TypeResolutionCache(), options, provider);

            // Act and assert
            Assert.NotNull(resolver.Get<CustomToken>());
        }

        [Fact]
        public void Get_ThrowsAnExceptionForInvalidEntityType()
        {
            // Arrange
            var services = new ServiceCollection();

            var options = Mock.Of<IOptionsMonitor<OpenIddictEntityFrameworkOptions>>();
            var provider = services.BuildServiceProvider();
            var resolver = new OpenIddictEntityFrameworkTokenStoreResolver(new TypeResolutionCache(), options, provider);

            // Act and assert
            var exception = Assert.Throws<InvalidOperationException>(() => resolver.Get<CustomToken>());

            Assert.Equal(new StringBuilder()
                .AppendLine("The specified token type is not compatible with the Entity Framework 6.x stores.")
                .Append("When enabling the Entity Framework 6.x stores, make sure you use the built-in ")
                .Append("'OpenIddictEntityFrameworkToken' entity or a custom entity that inherits ")
                .Append("from the generic 'OpenIddictEntityFrameworkToken' entity.")
                .ToString(), exception.Message);
        }

        [Fact]
        public void Get_ThrowsAnExceptionWhenDbContextTypeIsNotAvailable()
        {
            // Arrange
            var services = new ServiceCollection();

            var options = Mock.Of<IOptionsMonitor<OpenIddictEntityFrameworkOptions>>(
                mock => mock.CurrentValue == new OpenIddictEntityFrameworkOptions
                {
                    DbContextType = null
                });

            var provider = services.BuildServiceProvider();
            var resolver = new OpenIddictEntityFrameworkTokenStoreResolver(new TypeResolutionCache(), options, provider);

            // Act and assert
            var exception = Assert.Throws<InvalidOperationException>(() => resolver.Get<OpenIddictEntityFrameworkToken>());

            Assert.Equal(new StringBuilder()
                .AppendLine("No Entity Framework 6.x context was specified in the OpenIddict options.")
                .Append("To configure the OpenIddict Entity Framework 6.x stores to use a specific 'DbContext', ")
                .Append("use 'options.UseEntityFramework().UseDbContext<TContext>()'.")
                .ToString(), exception.Message);
        }

        [Fact]
        public void Get_ReturnsDefaultStoreCorrespondingToTheSpecifiedTypeWhenAvailable()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<IOpenIddictTokenStore<CustomToken>>());
            services.AddSingleton(CreateStore());

            var options = Mock.Of<IOptionsMonitor<OpenIddictEntityFrameworkOptions>>(
                mock => mock.CurrentValue == new OpenIddictEntityFrameworkOptions
                {
                    DbContextType = typeof(DbContext)
                });

            var provider = services.BuildServiceProvider();
            var resolver = new OpenIddictEntityFrameworkTokenStoreResolver(new TypeResolutionCache(), options, provider);

            // Act and assert
            Assert.NotNull(resolver.Get<MyToken>());
        }

        private static OpenIddictEntityFrameworkTokenStore<MyToken, MyApplication, MyAuthorization, DbContext, long> CreateStore()
            => new Mock<OpenIddictEntityFrameworkTokenStore<MyToken, MyApplication, MyAuthorization, DbContext, long>>(
                Mock.Of<IMemoryCache>(),
                Mock.Of<DbContext>(),
                Mock.Of<IOptionsMonitor<OpenIddictEntityFrameworkOptions>>()).Object;

        public class CustomToken { }

        public class MyApplication : OpenIddictEntityFrameworkApplication<long, MyAuthorization, MyToken> { }
        public class MyAuthorization : OpenIddictEntityFrameworkAuthorization<long, MyApplication, MyToken> { }
        public class MyScope : OpenIddictEntityFrameworkScope<long> { }
        public class MyToken : OpenIddictEntityFrameworkToken<long, MyApplication, MyAuthorization> { }
    }
}
