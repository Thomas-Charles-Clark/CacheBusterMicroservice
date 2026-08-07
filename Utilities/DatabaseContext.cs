using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Dapper;
using static Dapper.SqlMapper;

namespace CacheBusterMicroservice.Utilities
{
	public class DatabaseContext
	{
		private readonly string _connectionString;

		public DatabaseContext(string connectionString)
		{
			_connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
		}

		public async IAsyncEnumerable<string> StreamProductPageURLs(
			object? parameters = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			using var connection = new SqlConnection(_connectionString);
			await connection.OpenAsync(cancellationToken);

			using var command = connection.CreateCommand();
			command.CommandText = "cache.FloodTester_ProductPageURLs_Select";
			command.CommandType = CommandType.StoredProcedure;

			//#if DEBUG
			//			command.Parameters.AddWithValue("@BaseURL", "https://flock-dev.designersguild.com");
			//#else
			//			command.Parameters.AddWithValue("@BaseURL", "https://www.designersguild.com");
			//#endif

			command.Parameters.AddWithValue("@BaseURL", "https://flock-dev.designersguild.com");


			// SequentialAccess forces data streaming directly from the network packets
			using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

			while (await reader.ReadAsync(cancellationToken))
			{
				if (!await reader.IsDBNullAsync(0, cancellationToken))
				{
					// Yields each individual URL row immediately
					yield return reader.GetString(0); 
				}
			}
		}
	}
}
