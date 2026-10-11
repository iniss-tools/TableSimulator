using TableSimulator.Application.Faces;

namespace TableSimulator.Tests.Application;

/// <summary>Vlastne predlohy ciel v pamati - namiesto databazy v testoch aplikacnej vrstvy.</summary>
internal sealed class MemoryFaceRepository : IFaceRepository
{
    public Dictionary<string, FaceRecord> Faces { get; } = new(StringComparer.Ordinal);

    public Task<IReadOnlyList<FaceRecord>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FaceRecord>>(Faces.Values.OrderBy(f => f.Id, StringComparer.Ordinal).ToList());

    public Task<FaceRecord?> FindAsync(string id, CancellationToken cancellationToken) => Task.FromResult(Faces.GetValueOrDefault(id));

    public Task SaveAsync(FaceRecord face, CancellationToken cancellationToken)
    {
        Faces[face.Id] = face;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken) => Task.FromResult(Faces.Remove(id));
}
