using System.Numerics;
using System.Reflection;
using Newtonsoft.Json;
using Ssit.CrossX2.Framework.Games.Editor;
using Ssit.CrossX2.Framework.Games.Template;

namespace Ssit.CrossX2.Framework.Games.Map;

public class MapObject
{
    public int Id { get; set; }
    public string TypeId { get; set; }
    public bool HasLogic { get; set; }

    [EditorInt(-1000, 1000, 10)]
    public int ZOrder { get; set; }
    
    public Vector2 Position { get; set; }
    
    public Type Type { get; set; }
    
    [Editor]
    public bool Flipped { get; set; }
    
    [EditorComplex]
    public object ParametersObject { get; set; }

    public List<int> Links { get; private set; }

    internal static MapObject Load(BinaryReader reader, IGameTemplate gameTemplate)
    {
        var id = reader.ReadInt32();
        var hasLogic = reader.ReadBoolean();
        var typeId = reader.ReadString();
        var parameters = reader.ReadString();
        var posX = reader.ReadSingle();
        var posY = reader.ReadSingle();
        var flipped = reader.ReadBoolean();
        var zOrder = reader.ReadInt32();

        Type parametersType;
        Type type = null;
        
        if (hasLogic)
        {
            var obj = gameTemplate.Objects.First(o => o.FullName == typeId);
            parametersType = obj.ParametersType;
            
            type = obj.TargetType;
        }
        else
        {
            parametersType = typeof(StaticObjectParameters);
        }

        object paramsObj;

        try
        {
            paramsObj = JsonConvert.DeserializeObject(parameters, parametersType);
        }
        catch
        {
            paramsObj = Activator.CreateInstance(parametersType);
        }

        var mo = new MapObject
        {
            Id = id,
            HasLogic = hasLogic,
            TypeId = typeId,
            ParametersObject = paramsObj,
            Position = new(posX, posY),
            Flipped = flipped,
            Type = type,
            ZOrder = zOrder,
        };
        mo.UpdateLinks();
        return mo;
    }
    
    public void Save(BinaryWriter writer)
    {
        var parameters = ParametersObject == null ? "" : JsonConvert.SerializeObject(ParametersObject);
        
        writer.Write(Id);
        writer.Write(HasLogic);
        writer.Write(TypeId);
        writer.Write(parameters);
        writer.Write(Position.X);
        writer.Write(Position.Y);
        writer.Write(Flipped);
        writer.Write(ZOrder);
    }
    
    public void UpdateLinks()
    {
        if (ParametersObject is null)
            return;
        
        var properties = ParametersObject.GetType().GetProperties(BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public);
        
        Links?.Clear();
        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<EditorLinkAttribute>() is not null)
            {
                var val = property.GetValue(ParametersObject);

                if (val is int i)
                {
                    if (Links is null)
                    {
                        Links = new();
                    }
                    Links.Add(i);
                }
            }
        }
    }
}