using System.Text.Json;
using System.Text.Json.Serialization;
using API.Models.DTOs;

namespace API.Converters;

/// <summary>
/// Custom JSON converter for EmployeeDto to handle Guid id from JSON
/// Supports case-insensitive property names from external sources
/// </summary>
public class EmployeeDtoJsonConverter : JsonConverter<EmployeeDto>
{
    public override EmployeeDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected object");

        var employee = new EmployeeDto();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var propertyName = reader.GetString()?.ToLowerInvariant();
            reader.Read();

            switch (propertyName)
            {
                case "id":
                    // Handle id as Guid string
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        var idStr = reader.GetString();
                        if (Guid.TryParse(idStr, out var guidId))
                            employee.Id = guidId;
                    }
                    break;

                case "employeecode":
                    employee.EmployeeCode = reader.GetString() ?? string.Empty;
                    break;

                case "fullname":
                    employee.FullName = reader.GetString() ?? string.Empty;
                    break;

                case "status":
                    employee.Status = reader.GetString() ?? string.Empty;
                    break;

                case "createdat":
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        if (DateTime.TryParse(reader.GetString(), out var createdAt))
                            employee.CreatedAt = createdAt;
                    }
                    break;

                default:
                    // Skip unknown properties
                    break;
            }
        }

        return employee;
    }

    public override void Write(Utf8JsonWriter writer, EmployeeDto value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("id", value.Id.ToString());
        writer.WriteString("employeeCode", value.EmployeeCode);
        writer.WriteString("fullName", value.FullName);
        writer.WriteString("status", value.Status);
        writer.WriteString("createdAt", value.CreatedAt.ToString("O"));
        writer.WriteEndObject();
    }
}
