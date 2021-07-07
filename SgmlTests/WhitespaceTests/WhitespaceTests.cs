﻿/*
 * Original work Copyright (c) 2008 MindTouch. All rights reserved. 
 * Modified Work Copyright (c) 2016 Microsoft Corporation. All rights reserved.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 */

using Sgml;
using System;
using System.Linq;
using System.IO;
using System.Xml;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Collections.Generic;

namespace SGMLTests
{

    /// <summary>https://github.com/lovettchris/SgmlReader/issues/15</summary>
    [TestFixture]
    public class WhitespaceTests
    {
        [Test]
        public void Invalid_TextWhitespace_enum_values_should_be_dropped()
        {
            SgmlReader sgmlReader = new SgmlReader()
            {
                TextWhitespace = (TextWhitespaceHandling)0xFF
            };

            Assert.AreEqual(TextWhitespaceHandling.TrimBoth | TextWhitespaceHandling.OnlyLineBreaks, sgmlReader.TextWhitespace, "The " + nameof(sgmlReader.TextWhitespace) + " property should only respect defined flags bits.");
        }

        [Test]
        public void DoBradescoTrailingWhitespaceTest()
        {
            // bradesco.ofx is licensed under the MIT license. From github.com/kevencarneiro/OFXSharp
            string bradescoOfxText = Tests.ReadTestResource(name: "bradesco.ofx");
            bradescoOfxText = bradescoOfxText.Substring(startIndex: 134); // skip first 134 chars of OFX header.

            SgmlDtd ofx160Dtd      = Tests.LoadDtd(docType: "OFX", name: "ofx160.dtd");

            XmlDocument asXml = ConvertSgmlToXml(ofx160Dtd, bradescoOfxText);

            XmlNode codeNode = asXml.SelectSingleNode("//*[local-name()='CODE']");

            XmlNodeList memoNodes = asXml.SelectNodes("//*[local-name()='MEMO']");

            List<XmlNode> selectedNodes = new List<XmlNode>();
            selectedNodes.Add(codeNode);
            selectedNodes.AddRange(memoNodes.Cast<XmlNode>());

            foreach (XmlNode node in selectedNodes)
            {
                string innerText = codeNode.InnerText;
                bool hasTrailingLineBreak = innerText.EndsWith("\n");

                Assert.IsFalse(hasTrailingLineBreak, message: "There should be no trailing line-breaks in <" + node.Name + "> elements' innerText.");
            }
        }

        private static XmlDocument ConvertSgmlToXml(SgmlDtd sgmlDtd, string sgmlText)
        {
            using (StringReader stringReader = new StringReader(sgmlText))
            {
                SgmlReader sgmlReader = new SgmlReader()
                {
                    InputStream        = stringReader,
                    WhitespaceHandling = WhitespaceHandling.None,
                    TextWhitespace     = TextWhitespaceHandling.OnlyTrailingLineBreaks,
                    DocType            = sgmlDtd.Name, // "OFX"; ?
                    Dtd                = sgmlDtd
                };

                Assert.AreEqual(TextWhitespaceHandling.OnlyTrailingLineBreaks, sgmlReader.TextWhitespace, "The " + nameof(sgmlReader.TextWhitespace) + " property should persist this valid value.");

                XmlDocument doc = new XmlDocument();
                using (XmlWriter xmlWriter = doc.CreateNavigator().AppendChild())
                {
                    while (!sgmlReader.EOF)
                    {
                        xmlWriter.WriteNode(sgmlReader, defattr: true);
                    }
                }

                return doc;
            }
        }
    }
}
