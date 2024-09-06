using System.Collections.ObjectModel;

namespace NDiscoPlus.LightHandlers;

public struct ValidationErrorCollector
{
    private List<string>? errors;

    public ValidationErrorCollector()
    {
        errors = new();
    }

    public readonly void Add(string msg)
    {
        if (errors is null)
            throw new InvalidOperationException("Cannot add new errors after Collect() is called.");

        errors.Add(msg);
    }

    public IList<string> Collect()
    {
        if (errors is null)
            throw new InvalidOperationException("Cannot call Collect() twice.");

        ReadOnlyCollection<string> err = errors.AsReadOnly();
        errors = null;
        return err;
    }
}