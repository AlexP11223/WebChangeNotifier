using System;
using System.Linq;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace WebChangeNotifier
{
    public class DiffResult
    {
        private readonly DiffPaneModel _diffPaneModel;

        public DiffResult(DiffPaneModel diffPaneModel)
        {
            _diffPaneModel = diffPaneModel;
        }

        public string DiffText => String.Join("\r\n", _diffPaneModel.Lines.Select(line =>
            {
                switch (line.Type)
                {
                    case ChangeType.Inserted:
                        return "+ " + line.Text;
                    case ChangeType.Deleted:
                        return "- " + line.Text;
                    default:
                        return "  " + line.Text;
                }
            }));

        public int InsertedCount => _diffPaneModel.Lines.Count(line => line.Type == ChangeType.Inserted);

        public int DeletedCount => _diffPaneModel.Lines.Count(line => line.Type == ChangeType.Deleted);
    }

    public class Differ
    {
        private readonly InlineDiffBuilder _diffBuilder = new InlineDiffBuilder(new DiffPlex.Differ());

        public DiffResult Diff(string before, string after)
        {
            return new DiffResult(_diffBuilder.BuildDiffModel(before, after));
        }
    }
}
