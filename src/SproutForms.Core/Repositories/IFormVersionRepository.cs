using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Repositories
{
    public interface IFormVersionRepository
    {
        FormVersion? GetPublished(Guid formId);
        FormVersion? GetLatest(Guid formId);
        FormVersion? Get(Guid formVersionId);

        // Newest first
        IReadOnlyList<FormVersion> GetAll(Guid formId);

        void Add(FormVersion version);
        void Publish(Guid versionId);
        void DeleteAllByForm(Guid formId);
    }
}
