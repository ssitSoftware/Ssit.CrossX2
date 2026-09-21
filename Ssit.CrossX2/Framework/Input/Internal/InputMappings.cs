using Ssit.CrossX2.Framework.IoC;

namespace Ssit.CrossX2.Framework.Input.Internal;

internal class InputMappings: IInputMappings, IInputMappingsInt
{
    private readonly IIoCContainer _container;
    private readonly Dictionary<int, InputMapping> _mappings = new();

    public InputMappings(IIoCContainer container)
    {
        _container = container;
    }
    
    private InputMapping GetMapping(int player, bool create)
    {
        if (!_mappings.TryGetValue(player, out var mapping))
        {
            if (!create) return null;
            
            mapping = _container.IoCConstruct<InputMapping>(player);
            _mappings.Add(player, mapping);
        }

        return mapping;
    }

    public IMapper Mapper(int player) => GetMapping(player, true);
    public IInputMapping this[int player] => GetMapping(player, false) ?? throw new InvalidOperationException();
    public int[] MappedPlayers => _mappings.Keys.ToArray();
}