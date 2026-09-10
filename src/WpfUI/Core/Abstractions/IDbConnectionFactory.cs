// Core/Abstractions/IDbConnectionFactory.cs
using System.Data;

namespace WPFUI.Core.Abstractions;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
