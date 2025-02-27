﻿using Hypercube.Graphics.Shaders;
using Hypercube.OpenGL.Shaders;
using Hypercube.Shared.Dependency;
using Hypercube.Shared.Resources;
using Hypercube.Shared.Resources.Manager;

namespace Hypercube.Client.Resources.Caching;

public sealed class ShaderSourceResource : Resource, IDisposable
{
    public IShaderProgram ShaderProgram = default!;
    
    public string Base;
    public ResourcePath VertexPath;
    public ResourcePath FragmentPath;

    public ShaderSourceResource()
    {
        Base = string.Empty;
        VertexPath = string.Empty;
        FragmentPath = string.Empty;
    }
    
    public ShaderSourceResource(ResourcePath path)
    {
        Base = path;
        VertexPath = $"{path}.vert";
        FragmentPath =  $"{path}.frag";
    }
    
    protected override void OnLoad(ResourcePath path, DependenciesContainer container)
    {
        var resourceLoader = container.Resolve<IResourceLoader>();
        var vertSource = resourceLoader.ReadFileContentAllText($"{path}.vert");
        var fragSource = resourceLoader.ReadFileContentAllText($"{path}.frag");
        
        ShaderProgram = new ShaderProgram(vertSource, fragSource);
    }

    public void Dispose()
    {
        ShaderProgram.Dispose();
    }
}