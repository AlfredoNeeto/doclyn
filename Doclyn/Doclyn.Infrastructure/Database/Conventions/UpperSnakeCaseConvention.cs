using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.Text;

namespace Doclyn.Infrastructure.Database.Conventions;

/// <summary>
/// Convenção do EF Core que converte todos os nomes de banco de dados
/// de PascalCase para UPPER_SNAKE_CASE automaticamente.
/// Aplica-se a tabelas, colunas, chaves, chaves estrangeiras e índices.
/// </summary>
public sealed class UpperSnakeCaseConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        var model = modelBuilder.Metadata;

        foreach (var entityType in model.GetEntityTypes())
        {
            // Tabela
            var tableName = entityType.GetTableName();
            if (!string.IsNullOrEmpty(tableName))
            {
                entityType.SetTableName(ConvertToUpperSnakeCase(tableName));
            }

            // Colunas
            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName();
                if (!string.IsNullOrEmpty(columnName))
                {
                    property.SetColumnName(ConvertToUpperSnakeCase(columnName));
                }
            }

            // Chaves primárias e alternativas
            foreach (var key in entityType.GetKeys())
            {
                var keyName = key.GetName();
                if (!string.IsNullOrEmpty(keyName))
                {
                    key.SetName(ConvertToUpperSnakeCase(keyName));
                }
            }

            // Chaves estrangeiras
            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                var fkName = foreignKey.GetConstraintName();
                if (!string.IsNullOrEmpty(fkName))
                {
                    foreignKey.SetConstraintName(ConvertToUpperSnakeCase(fkName));
                }
            }

            // Índices
            foreach (var index in entityType.GetIndexes())
            {
                var indexName = index.GetDatabaseName();
                if (!string.IsNullOrEmpty(indexName))
                {
                    index.SetDatabaseName(ConvertToUpperSnakeCase(indexName));
                }
            }
        }
    }

    private static string ConvertToUpperSnakeCase(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var result = new StringBuilder();

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];

            if (char.IsUpper(c))
            {
                if (i > 0 &&
                    (char.IsLower(value[i - 1]) ||
                     (i + 1 < value.Length && char.IsLower(value[i + 1]))))
                {
                    result.Append('_');
                }

                result.Append(c);
            }
            else
            {
                result.Append(char.ToUpperInvariant(c));
            }
        }

        return result.ToString();
    }
}
