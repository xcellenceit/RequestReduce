﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using RequestReduce.Configuration;
using RequestReduce.Utilities;

namespace RequestReduce.Reducer
{
    public class Reducer : IReducer
    {
        private readonly IWebClientWrapper webClientWrapper;
        private IConfigurationWrapper configWrapper;
        private IFileWrapper fileWrapper;
        private IMinifier minifier;
        private ISpriteManager spriteManager;
        private ICssImageTransformer cssImageTransformer;
        private readonly HttpContextBase httpContextWrapper;

        public Reducer(IWebClientWrapper webClientWrapper, IConfigurationWrapper configWrapper, IFileWrapper fileWrapper, IMinifier minifier, ISpriteManager spriteManager, ICssImageTransformer cssImageTransformer, HttpContextBase httpContextWrapper)
        {
            this.webClientWrapper = webClientWrapper;
            this.httpContextWrapper = httpContextWrapper;
            this.cssImageTransformer = cssImageTransformer;
            this.spriteManager = spriteManager;
            this.minifier = minifier;
            this.fileWrapper = fileWrapper;
            this.configWrapper = configWrapper;
        }

        public virtual string Process(string urls)
        {
            var urlList = SplitUrls(urls);
            var fileName = string.Format("{0}/{1}.css", configWrapper.SpriteDirectory, Guid.NewGuid().ToString());
            var mergedCss = new StringBuilder();
            foreach (var url in urlList)
                mergedCss.Append(ProcessCss(url));
            fileWrapper.Save(minifier.Minify(mergedCss.ToString()), httpContextWrapper.Server.MapPath(fileName));
            return fileName;
        }

        protected virtual string ProcessCss(string url)
        {
            var cssContent = webClientWrapper.DownloadString(url);
            var imageUrls = cssImageTransformer.ExtractImageUrls(cssContent);
            foreach (var imageUrl in imageUrls)
            {
                var sprite = spriteManager.Add(imageUrl);
                cssContent = cssImageTransformer.InjectSprite(cssContent, imageUrl, sprite);
            }
            spriteManager.Flush();
            return cssContent;
        }

        protected static IEnumerable<string> SplitUrls(string urls)
        {
            return urls.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries);
        }

    }
}