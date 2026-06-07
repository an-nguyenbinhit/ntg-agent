using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.AI;
using System.Linq.Expressions;

namespace AskHR.Orchestrator.Data;

public class ChatRoleValueConverter : ValueConverter<ChatRole, string>
{
    public ChatRoleValueConverter(ConverterMappingHints? mappingHints = null) :
        base(
            value => value.Value,
            value => new ChatRole(value),
            mappingHints
        ) { }
}
