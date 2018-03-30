﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bit0.CrunchLog.Config;
using Bit0.CrunchLog.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Bit0.CrunchLog.Repositories
{
    public interface IContentProvider
    {
        IDictionary<FileInfo, Content> AllContent { get; }
        IDictionary<FileInfo, Content> Posts { get; }
        IDictionary<String, IEnumerable<FileInfo>> PostTags { get; }
        IDictionary<String, IEnumerable<FileInfo>> PostCategories { get; }
        IDictionary<String, IEnumerable<FileInfo>> PostArchives { get; }

    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class ContentProvider : IContentProvider
    {
        private readonly CrunchConfig _config;
        private readonly JsonSerializer _jsonSerializer;
        private readonly ILogger<ContentProvider> _logger;

        public ContentProvider(JsonSerializer jsonSerializer, CrunchConfig config, ILogger<ContentProvider> logger)
        {
            _logger = logger;
            _config = config;
            _jsonSerializer = jsonSerializer;
        }

        public IDictionary<FileInfo, Content> AllContent
        {
            get
            {
                var list = new Dictionary<FileInfo, Content>();

                foreach (var metaFile in _config.BasePath.GetFiles("*.json", SearchOption.AllDirectories))
                {
                    var content = new Content
                    {
                        MetaFile = metaFile,
                        PermaLink = _config.Permalink
                    };

                    _jsonSerializer.Populate(metaFile?.OpenText(), content);

                    if (content.PermaLink == _config.Permalink)
                    {
                        content.PermaLink = content.GetFullSlug();
                    }

                    list.Add(metaFile, content);
                }

                _logger.LogDebug($"Fount {list.Count} documents");

                return list;
            }
        }

        public IDictionary<FileInfo, Content> Posts
        {
            get
            {
                return AllContent
                    .Where(x => x.Value.Layout == Layouts.Post)
                    .OrderByDescending(x => x.Value.Date)
                    .ThenByDescending(x => x.Value.Slug)
                    .ToDictionary(k => k.Key, v => v.Value);
            }
        }

        public IDictionary<String, IEnumerable<FileInfo>> PostTags
        {
            get
            {
                return Posts
                    .SelectMany(x => x.Value.Tags)
                    .Distinct()
                    .ToDictionary(
                        k => k, 
                        v => AllContent
                            .Where(x => x.Value.Tags.Contains(v))
                            .Select(x => x.Key)
                    );
            }
        }

        public IDictionary<String, IEnumerable<FileInfo>> PostCategories
        {
            get
            {
                return Posts
                    .SelectMany(x => x.Value.Categories)
                    .Distinct()
                    .ToDictionary(
                        k => k, 
                        v => AllContent
                            .Where(x => x.Value.Categories.Contains(v))
                            .Select(x => x.Key)
                    );
            }
        }

        public IDictionary<String, IEnumerable<FileInfo>> PostArchives
        {
            get
            {
                var archives = new Dictionary<String, IEnumerable<FileInfo>>();

                var permaLinks = Posts
                    .Select(x => x.Value.PermaLink.Split('/'))
                    .ToList();

                var years = permaLinks
                    .Select(x => x[1])
                    .Distinct();

                foreach (var year in years)
                {
                    var ySlug = $"/{year}/";
                    archives.Add(ySlug, 
                        Posts.Where(x => x.Value.PermaLink.StartsWith(ySlug))
                            .Select( x => x.Key));

                    var months = permaLinks
                        .Where(x => x[1] == year)
                        .Select(x => x[2])
                        .Distinct();

                    foreach (var month in months)
                    {
                        var mSlug = $"/{year}/{month}/";
                        archives.Add(mSlug, 
                            Posts.Where(x => x.Value.PermaLink.StartsWith(mSlug))
                                .Select( x => x.Key));

                        var days = permaLinks
                            .Where(x => x[1] == year && x[2] == month)
                            .Select(x => x[3])
                            .Distinct();

                        foreach (var day in days)
                        {
                            var dSlug = $"/{year}/{month}/{day}/";
                            archives.Add(dSlug,
                                Posts.Where(x => x.Value.PermaLink.StartsWith(dSlug))
                                    .Select(x => x.Key));
                        }
                    }
                }

                return archives;
            }
        }
    }
}