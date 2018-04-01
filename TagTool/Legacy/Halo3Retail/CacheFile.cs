using TagTool.Cache;
using TagTool.Common;
using TagTool.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;
using TagTool.Serialization;
using TagTool.Tags.Definitions;

namespace TagTool.Legacy.Halo3Retail
{
    public class CacheFile : Base.CacheFile
    {
        public CacheFile(FileInfo file, GameCacheContext cacheContext, CacheVersion version = CacheVersion.Halo3Retail)
            : base(file, version)
        {
            CacheContext = cacheContext;
            Header = new CacheHeader(this);
            IndexHeader = new CacheIndexHeader(this);
            IndexItems = new IndexTable(this);
            Strings = new StringTable(this);
            Resolver = new StringIdResolverHalo3();
            LocaleTables = new List<LocaleTable>();

            foreach (var language in Enum.GetValues(typeof(GameLanguage)))
                LocaleTables.Add(new LocaleTable(this, (GameLanguage)language));
        }

        new public class CacheHeader : Base.CacheFile.CacheHeader
        {
            public CacheHeader(Base.CacheFile Cache)
            {
                base.Cache = Cache;
                var Reader = base.Cache.Reader;

                #region Read Values
                XmlNode headerNode = base.Cache.versionNode.ChildNodes[0];
                XmlAttribute attr = headerNode.Attributes["fileSize"];
                int offset = int.Parse(attr.Value);
                Reader.SeekTo(offset);
                FileLength = Reader.ReadInt32();

                attr = headerNode.Attributes["indexOffset"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset);
                IndexOffset = Reader.ReadInt32();

                attr = headerNode.Attributes["tagDataAddress"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset);
                IndexStreamSize = Reader.ReadInt32();

                attr = headerNode.Attributes["stringCount"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset);
                StringIdsCount = Reader.ReadInt32();

                attr = headerNode.Attributes["stringTableSize"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset);
                StringIdsBufferSize = Reader.ReadInt32();

                attr = headerNode.Attributes["stringTableIndexOffset"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset);
                StringIdIndicesOffset = Reader.ReadInt32();

                attr = headerNode.Attributes["stringTableOffset"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset);
                StringIdsBufferOffset = Reader.ReadInt32();

                attr = headerNode.Attributes["scenarioName"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset);
                ScenarioPath = Reader.ReadString(256);

                attr = headerNode.Attributes["fileCount"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset);
                TagNamesCount = Reader.ReadInt32();

                attr = headerNode.Attributes["fileTableOffset"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset);
                TagNamesBufferOffset = Reader.ReadInt32();

                attr = headerNode.Attributes["fileTableSize"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset);
                TagNamesBufferSize = Reader.ReadInt32();

                attr = headerNode.Attributes["fileTableIndexOffset"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset);
                TagNameIndicesOffset = Reader.ReadInt32();

                attr = headerNode.Attributes["virtualBaseAddress"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset);
                VirtualBaseAddress = Reader.ReadInt32();
                attr = headerNode.Attributes["rawTableOffset"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset);
                RawTableOffset = Reader.ReadInt32();

                attr = headerNode.Attributes["localeModifier"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset);
                LocaleModifier = Reader.ReadInt32();

                attr = headerNode.Attributes["rawTableSize"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset);
                RawTableSize = Reader.ReadInt32();
                #endregion

                #region Modify Offsets
                if (RawTableOffset == 0)
                {
                    base.Cache.Magic = VirtualBaseAddress - IndexStreamSize;
                }
                else
                {

                    this.Magic = StringIdIndicesOffset - base.Cache.HeaderSize;

                    TagNamesBufferOffset -= this.Magic;
                    TagNameIndicesOffset -= this.Magic;
                    StringIdIndicesOffset -= this.Magic;
                    StringIdsBufferOffset -= this.Magic;

                    base.Cache.Magic = VirtualBaseAddress - (RawTableOffset + RawTableSize);
                }
                IndexOffset -= base.Cache.Magic;
                #endregion
            }
        }

        new public class CacheIndexHeader : Base.CacheFile.CacheIndexHeader
        {
            public CacheIndexHeader(Base.CacheFile Cache)
            {
                cache = Cache;
                var Reader = cache.Reader;

                #region Read Values
                XmlNode indexHeaderNode = cache.versionNode.ChildNodes[1];

                XmlAttribute attr = indexHeaderNode.Attributes["tagClassCount"];
                int offset = int.Parse(attr.Value);
                Reader.SeekTo(offset + cache.Header.IndexOffset);
                tagClassCount = Reader.ReadInt32();

                attr = indexHeaderNode.Attributes["tagInfoOffset"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset + cache.Header.IndexOffset);
                tagInfoOffset = Reader.ReadInt32() - cache.Magic;

                attr = indexHeaderNode.Attributes["tagClassIndexOffset"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset + cache.Header.IndexOffset);
                tagClassIndexOffset = Reader.ReadInt32() - cache.Magic;

                attr = indexHeaderNode.Attributes["tagCount"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset + cache.Header.IndexOffset);
                tagCount = Reader.ReadInt32();

                attr = indexHeaderNode.Attributes["tagInfoHeaderCount"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset + cache.Header.IndexOffset);
                tagInfoHeaderCount = Reader.ReadInt32();

                attr = indexHeaderNode.Attributes["tagInfoHeaderOffset"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset + cache.Header.IndexOffset);
                tagInfoHeaderOffset = Reader.ReadInt32() - cache.Magic;

                attr = indexHeaderNode.Attributes["tagInfoHeaderCount2"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset + cache.Header.IndexOffset);
                tagInfoHeaderCount2 = Reader.ReadInt32();

                attr = indexHeaderNode.Attributes["tagInfoHeaderOffset2"];
                offset = int.Parse(attr.Value);
                Reader.SeekTo(offset + cache.Header.IndexOffset);
                tagInfoHeaderOffset2 = Reader.ReadInt32() - cache.Magic;
                #endregion
            }
        }

        new public class IndexTable : Base.CacheFile.IndexTable
        {
            public IndexTable(Base.CacheFile Cache)
            {
                cache = Cache;

                var indexHeader = cache.IndexHeader;
                var cacheHeader = cache.Header;
                var reader = cache.Reader;

                ClassList = new List<TagClass>();

                #region Read Class List
                reader.SeekTo(indexHeader.tagClassIndexOffset);
                for (int i = 0; i < indexHeader.tagClassCount; i++)
                {
                    var tc = new TagClass()
                    {
                        ClassCode = reader.ReadString(4),
                        Parent = reader.ReadString(4),
                        Parent2 = reader.ReadString(4),
                        StringID = reader.ReadInt32()
                    };
                    ClassList.Add(tc);
                }
                #endregion

                #region Read Tags Info
                reader.SeekTo(indexHeader.tagInfoOffset);
                for (int i = 0; i < indexHeader.tagCount; i++)
                {
                    IndexItem item = new IndexItem() { Cache = cache };
                    item.ClassIndex = reader.ReadInt16();
                    item.ID = (reader.ReadInt16() << 16) | i;
                    item.Offset = reader.ReadInt32() - cache.Magic;
                    item.metaIndex = i;
                    this.Add(item);
                }
                #endregion

                #region Read Indices
                reader.SeekTo(cacheHeader.TagNameIndicesOffset);
                int[] indices = new int[indexHeader.tagCount];
                for (int i = 0; i < indexHeader.tagCount; i++)
                    indices[i] = reader.ReadInt32();
                #endregion

                #region Read Names
                reader.SeekTo(cacheHeader.TagNamesBufferOffset);

                EndianReader newReader = null;

                if (cache.TagsKey == "" || cache.TagsKey == null)
                {
                    newReader = new EndianReader(new MemoryStream(reader.ReadBytes(cacheHeader.TagNamesBufferSize)), EndianFormat.BigEndian);
                }
                else
                {
                    reader.BaseStream.Position = cacheHeader.TagNamesBufferOffset;
                    newReader = new EndianReader(reader.DecryptAesSegment(cacheHeader.TagNamesBufferSize, cache.TagsKey), EndianFormat.BigEndian);
                }

                for (int i = 0; i < indices.Length; i++)
                {
                    if (indices[i] == -1)
                    {
                        this[i].Filename = "<null>";
                        continue;
                    }

                    newReader.SeekTo(indices[i]);

                    int length;
                    if (i == indices.Length - 1)
                        length = cacheHeader.TagNamesBufferSize - indices[i];
                    else
                    {
                        if (indices[i + 1] == -1)
                        {
                            int index = -1;

                            for (int j = i + 1; j < indices.Length; j++)
                            {
                                if (indices[j] != -1)
                                {
                                    index = j;
                                    break;
                                }
                            }

                            length = (index == -1) ? cacheHeader.TagNamesBufferSize - indices[i] : indices[index] - indices[i];
                        }
                        else
                            length = indices[i + 1] - indices[i];
                    }

                    if (length == 1)
                    {
                        this[i].Filename = "<blank>";
                        continue;
                    }

                    if (length < 0)
                    {
                        int i0 = indices[i];
                        int i1 = indices[i + 1];
                        int i2 = indices[i + 2];
                        int i3 = indices[i + 3];
                    }

                    this[i].Filename = newReader.ReadString(length);
                }

                newReader.Close();
                newReader.Dispose();
                #endregion
            }
        }

        public override void LoadResourceTags()
        {
            TagDeserializer deserializer = new TagDeserializer(CacheVersion.Halo3Retail);

            foreach (IndexItem item in IndexItems)
            {
                if (item.ClassCode == "play")
                {
                    var blamContext = new CacheSerializationContext(CacheContext, this, item);
                    ResourceLayoutTable = deserializer.Deserialize<CacheFileResourceLayoutTable>(blamContext);

                    break;
                }
            }

            foreach (IndexItem item in IndexItems)
            {
                if (item.ClassCode == "zone")
                {
                    var blamContext = new CacheSerializationContext(CacheContext, this, item);
                    ResourceGestalt = deserializer.Deserialize<CacheFileResourceGestalt>(blamContext);

                    foreach(var tagresource in ResourceGestalt.TagResources)
                    {
                        foreach(var fixup in tagresource.ResourceFixups)
                        {
                            fixup.Offset = (fixup.Address & 0x0FFFFFFF);
                            fixup.Type = (fixup.Address >> 28) & 0xF;
                            fixup.RawAddress = fixup.Address;
                        }
                    }

                    break;
                }
            }
        }

        public override byte[] GetRawFromID(int ID, int DataLength)
        {
            if (ID == -1)
                return null;

            if (ResourceLayoutTable == null || ResourceGestalt == null)
                LoadResourceTags();

            EndianReader er;
            string fName = "";

            var Entry = ResourceGestalt.TagResources[ID & ushort.MaxValue];

            if (Entry.PlaySegmentIndex == -1) return null;

            var Loc = ResourceLayoutTable.Segments[Entry.PlaySegmentIndex];

            if (Loc.PrimaryPageIndex == -1 || Loc.PrimarySegmentOffset == -1)
            {
                Console.WriteLine($"Failed to find the raw at definition entry {ID & ushort.MaxValue} : resource offset and index are -1.");
                return null;
            }
                
            int index = (Loc.SecondaryPageIndex != -1) ? Loc.SecondaryPageIndex : Loc.PrimaryPageIndex;
            int locOffset = (Loc.SecondarySegmentOffset != -1) ? Loc.SecondarySegmentOffset : Loc.PrimarySegmentOffset;

            if (index == -1 || locOffset == -1) return null;

            if (ResourceLayoutTable.RawPages[index].BlockOffset == -1)
            {
                index = Loc.PrimaryPageIndex;
                locOffset = Loc.PrimarySegmentOffset;
            }

            var Pool = ResourceLayoutTable.RawPages[index];

            if (Pool.SharedCacheIndex != -1)
            {
                fName = ResourceLayoutTable.ExternalCacheReferences[Pool.SharedCacheIndex].MapPath;
                fName = fName.Substring(fName.LastIndexOf('\\'));
                fName = File.DirectoryName + fName;

                if (fName == File.FullName)
                    er = Reader;
                else
                {
                    var fs = new FileStream(fName, FileMode.Open, FileAccess.Read);
                    er = new EndianReader(fs, EndianFormat.BigEndian);
                }
            }
            else
                er = Reader;

            er.SeekTo(int.Parse(versionNode.ChildNodes[0].Attributes["rawTableOffset"].Value));
            int offset = Pool.BlockOffset + er.ReadInt32();
            er.SeekTo(offset);
            byte[] compressed = er.ReadBytes(Pool.CompressedBlockSize);
            byte[] decompressed = new byte[Pool.UncompressedBlockSize];

            BinaryReader BR = new BinaryReader(new DeflateStream(new MemoryStream(compressed), CompressionMode.Decompress));
            decompressed = BR.ReadBytes(Pool.UncompressedBlockSize);
            BR.Close();
            BR.Dispose();

            byte[] data = new byte[(DataLength != -1) ? DataLength : (Pool.UncompressedBlockSize - locOffset)];
            int length = data.Length;
   
            if (length > decompressed.Length)
                length = decompressed.Length;

            //Attempt to fix offset/lengths problem on some resources
            if (length > decompressed.Length - locOffset)
            {
                var remainder = length - (decompressed.Length - locOffset);
                Array.Copy(decompressed, locOffset, data, 0, length - remainder);
            }
            else
            {
                Array.Copy(decompressed, locOffset, data, 0, length);
            }


            Array.Copy(decompressed, locOffset, data, 0, length);

            if (er != Reader)
            {
                er.Close();
                er.Dispose();
            }

            return data;
        }

        public override byte[] GetSoundRaw(int ID, int size)
        {
            if (ResourceLayoutTable == null || ResourceGestalt == null)
                LoadResourceTags();

            var Entry = ResourceGestalt.TagResources[ID & ushort.MaxValue];

            if (Entry.PlaySegmentIndex == -1)
            {
                Console.WriteLine($"Segment index = -1 at definition entry {ID & ushort.MaxValue} ");
                return null;
            }
                
            

            var segment = ResourceLayoutTable.Segments[Entry.PlaySegmentIndex];

            if (segment.PrimaryPageIndex == -1 || segment.PrimarySegmentOffset == -1 ||  segment.PrimarySizeIndex == -1 || segment.SecondarySizeIndex == -1)
            {
                Console.WriteLine($"Failed to find the raw at definition entry {ID & ushort.MaxValue} : sound offset and index are -1.");
                return null;
            }
                

            var sRaw = ResourceLayoutTable.Sizes[segment.SecondarySizeIndex];
            var reqPage = ResourceLayoutTable.RawPages[segment.PrimaryPageIndex];
            var optPage = ResourceLayoutTable.RawPages[segment.SecondaryPageIndex];

            if (size == 0) size = (reqPage.CompressedBlockSize != 0) ? reqPage.CompressedBlockSize : optPage.CompressedBlockSize;

            var reqSize = size - sRaw.OverallSize;
            var optSize = size - reqSize;

            byte[] buffer;
            byte[] data = new byte[size];
            int offset;
            EndianReader er;
            string fName = "";

            #region REQUIRED
            if (reqSize > 0)
            {
                if (reqPage.SharedCacheIndex != -1)
                {
                    fName = ResourceLayoutTable.ExternalCacheReferences[reqPage.SharedCacheIndex].MapPath;
                    fName = fName.Substring(fName.LastIndexOf('\\'));
                    fName = File.DirectoryName + fName;

                    if (fName == File.FullName)
                        er = Reader;
                    else
                        er = new EndianReader(new FileStream(fName, FileMode.Open, FileAccess.Read), EndianFormat.BigEndian);
                }
                else
                    er = Reader;

                er.SeekTo(1136);
                offset = reqPage.BlockOffset + er.ReadInt32();

                er.SeekTo(offset);
                buffer = er.ReadBytes(reqPage.CompressedBlockSize);

                Array.Copy(buffer, segment.PrimarySegmentOffset, data, 0, reqSize);

                if (er != Reader)
                {
                    er.Close();
                    er.Dispose();
                }
            }
            #endregion

            #region OPTIONAL
            if (segment.SecondaryPageIndex != -1 && optSize > 0)
            {
                if (optPage.SharedCacheIndex != -1)
                {
                    fName = ResourceLayoutTable.ExternalCacheReferences[optPage.SharedCacheIndex].MapPath;
                    fName = fName.Substring(fName.LastIndexOf('\\'));
                    fName = File.DirectoryName + fName;

                    if (fName == File.FullName)
                        er = Reader;
                    else
                        er = new EndianReader(new FileStream(fName, FileMode.Open, FileAccess.Read), EndianFormat.BigEndian);
                }
                else
                    er = Reader;

                er.SeekTo(1136);
                offset = optPage.BlockOffset + er.ReadInt32();

                er.SeekTo(offset);
                buffer = er.ReadBytes(optPage.CompressedBlockSize);

                if (buffer.Length > data.Length)
                    data = buffer;
                else
                    Array.Copy(buffer, segment.SecondarySegmentOffset, data, reqSize, optSize);


                if (er != Reader)
                {
                    er.Close();
                    er.Dispose();
                }
            }
            #endregion

            return data;
        }
    }
}
