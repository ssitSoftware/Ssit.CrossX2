namespace CrossX2.Graphics;

public interface IGameRendererBuilder
{
    IGameRendererBuilder WithComponent(uint moduleId, object parameters = null);
    IGameRendererBuilder WithCustomModule<TModule>(uint moduleId, object parameters = null);
    IGameRendererBuilder WithRenderHost(RenderHostParameters parameters);
}