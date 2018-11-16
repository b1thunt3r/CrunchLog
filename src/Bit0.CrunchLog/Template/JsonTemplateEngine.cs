﻿using Bit0.CrunchLog.Config;
using Bit0.CrunchLog.Extensions;
using Bit0.CrunchLog.Template.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace Bit0.CrunchLog.Template
{
    public class JsonTemplateEngine : ITemplateEngine
    {
        private readonly CrunchSite _siteConfig;

        public JsonTemplateEngine(CrunchSite siteConfig)
        {
            _siteConfig = siteConfig;
        }

        public void Render(ITemplateModel model)
        {
            var outputDir = _siteConfig.Paths.OutputPath.CombineDirPath(model.Permalink.Replace("//", "/").Substring(1));
            Render(model, outputDir, "index");
        }

        public void Render(SiteTemplateModel model)
        {
            var outputDir = _siteConfig.Paths.OutputPath;
            Render(model, outputDir, "siteInfo");
        }

        private static void Render<T>(T model, DirectoryInfo outputDir, String name) where T : class
        {
            if (!outputDir.Exists)
            {
                outputDir.Create();
            }

            var file = outputDir.CombineFilePath(".json", name);

            using (var sw = file.CreateText())
            {
                sw.Write(JsonConvert.SerializeObject(model));
            }
        }
    }
}