/*
 * https://pastebin.com/xdyQikfr
 * https://gist.github.com/alexguirre/af70f0122957f005a5c12bef2618a786
 * 
 * https://gta.fandom.com/wiki/IDE
 * Item definition files, known by the extension .ide, are usually used to assign a model and 
 * texture file to a unique object ID, along with many parameters depending on the section. 
 * These files are in human readable text format, and allow the # character to comment lines out. 
 * IDE files can easily be opened and edited using any text-editing program like Notepad.
 * 
 * OBJS: Used to define standard static map objects.
 * 
 * objs
 * ModelName, TextureName, DrawDistance, Flag1, Flag2, (Bounds min)X,Y,Z, (Bounds max)X,Y,Z, (Bounds Sphere)X, Y, Z, Radius, WDD
 * end
 * 
 * Identifier	    Description
 *  ModelName       name of the .wdr model file, without extension (string)
 *  TextureName     name of the .wtd texture dictionary, without extension (string)
 *  DrawDistance    draw distance in units, one for each sub object (float)
 *  Flag1           object flag, defining special behavior
 *  Flag2           object flag, defining special behavior, default 0 (integer)
 *  Bounds Min      Lower Left vertex local position of a model bounding box
 *  Bounds Max      Upper Right vertex local position of a model bounding box
 *  Radius          Radius dimensions of the bounding Sphere
 *  WDD             the model dictionary file that contains the LOD model for the defined Modelname
*/

/*
 * https://web.archive.org/web/20140806111334/http://gtaforums.com/topic/549780-creating-maps-in-gta-iv-new-to-this/#entry1062341597
 * 
 * # GTA IV Binary Placement File (the # stops the line from being read)
 * version 3
 * inst
 * x_coord, y_coord, z_coord, rot_x, rot_y, rot_z, rot_w, modelname, 384, -1, 0, -1
 * end
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Xml;

namespace Coords2YMAP
{
    public partial class Form1 : Form
    {
        private object previusSelectedParticleEffect;
        //private string deskDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        public Form1()
        {
            InitializeComponent();
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //comboBox1.Items.Clear();
            //comboBox1.Items.Add("col_gen_tree_dust");
            comboBox1.SelectedIndex = 1;
        }

        private UInt32 Jenkins_one_at_a_time_hash(string key, int length)
        {
            int i = 0;
            UInt32 hash = 0;
            while (i != length)
            {
                hash += key[i++];
                hash += hash << 10;
                hash ^= hash >> 6;
            }
            hash += hash << 3;
            hash ^= hash >> 11;
            hash += hash << 15;
            return hash;
        }

        private void YMAP_Button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog FBD = new FolderBrowserDialog();

            if (FBD.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            var sourceIde = Directory.GetFiles(FBD.SelectedPath, "*.ide");
            var ideMapping = new Dictionary<string, string[]>();

            foreach (var ideFile in sourceIde)
            {
                // read the source IDE
                var lines = File.ReadAllLines(ideFile);
                var inObjs = false;

                foreach (var line in lines)
                {
                    if (line == "objs")
                    {
                        inObjs = true;
                    }
                    else if (inObjs)
                    {
                        if (line == "end")
                        {
                            break;
                        }

                        var bits = line.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

                        if (bits.Last() == "null")
                        {
                            ideMapping[bits[0].ToLower()] = bits;
                        }
                    }
                }

                var sourceOPLs = Directory.GetFiles(FBD.SelectedPath, "*.opl");
                var oplMapping = new List<string[]>();

                // read the source OPLs
                foreach (var opl in sourceOPLs)
                {
                    lines = File.ReadAllLines(opl);

                    inObjs = false;

                    foreach (var line in lines)
                    {
                        if (line == "inst")
                        {
                            inObjs = true;
                        }
                        else if (inObjs)
                        {
                            if (line == "end")
                            {
                                break;
                            }

                            var bits = line.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

                            if (ideMapping.ContainsKey(bits[7].ToLower()) || bits[7].StartsWith("hash:"))
                            {
                                oplMapping.Add(bits);
                            }
                        }
                    }
                }

                //Create xml document
                XmlDocument YMAPxml = new XmlDocument();

                //Root of xml tree
                XmlElement init = YMAPxml.CreateElement("entities");
                YMAPxml.AppendChild(init);

                //Copyright
                XmlComment newComment = YMAPxml.CreateComment("IDE&OPL2XML by indirivacua");
                YMAPxml.AppendChild(newComment);

                foreach (var entry in oplMapping)
                {
                    XmlElement itemRoot = YMAPxml.CreateElement("Item");
                    itemRoot.SetAttribute("type", "CEntityDef");
                    init.AppendChild(itemRoot);

                    XmlElement modelName = YMAPxml.CreateElement("hash_A023A02C");
                    if (entry[7].ToLower().IndexOf("hash:") == -1) //njliberty.opl
                    {
                        modelName.AppendChild(YMAPxml.CreateTextNode(entry[7].ToLower()));//entry.Value[0]
                    }
                    else //tokyoway.opl
                    {
                        var sourceWDRs = Directory.GetFiles(FBD.SelectedPath, "*.wdr");
                        foreach (var wdrFile in sourceWDRs)
                        {
                            string fileNameWithExtension = wdrFile.ToString().Substring(wdrFile.ToString().LastIndexOf("\\") + 1);
                            string fileNameWithoutExtension = fileNameWithExtension.Substring(0, fileNameWithExtension.Length - 4);
                            if (Jenkins_one_at_a_time_hash(fileNameWithoutExtension, fileNameWithoutExtension.Length) == Convert.ToInt64(entry[7].ToLower().Substring(5))) //hash:75089779
                            {
                                modelName.AppendChild(YMAPxml.CreateTextNode(fileNameWithoutExtension.ToLower()));
                            }
                        }
                    }
                    itemRoot.AppendChild(modelName);

                    XmlElement pos = YMAPxml.CreateElement("position");
                    //pos.AppendChild(YMAPxml.CreateTextNode(""));
                    pos.SetAttribute("x", "" + float.Parse(entry[0]));
                    pos.SetAttribute("y", "" + float.Parse(entry[1]));
                    pos.SetAttribute("z", "" + float.Parse(entry[2]));
                    itemRoot.AppendChild(pos);

                    XmlElement rotation = YMAPxml.CreateElement("rotation");
                    rotation.SetAttribute("x", "" + float.Parse(entry[3]));
                    rotation.SetAttribute("y", "" + float.Parse(entry[4]));
                    rotation.SetAttribute("z", "" + float.Parse(entry[5]));
                    rotation.SetAttribute("w", "" + float.Parse(entry[6]));
                    itemRoot.AppendChild(rotation);

                    XmlElement flags = YMAPxml.CreateElement("flags");
                    flags.SetAttribute("value", "1572868");
                    itemRoot.AppendChild(flags);

                    XmlElement guid = YMAPxml.CreateElement("guid");
                    guid.SetAttribute("value", "3800200077");
                    itemRoot.AppendChild(guid);

                    XmlElement hash_9CA32637 = YMAPxml.CreateElement("hash_9CA32637");
                    hash_9CA32637.SetAttribute("value", "1,0");
                    itemRoot.AppendChild(hash_9CA32637);

                    XmlElement hash_10FB7C42 = YMAPxml.CreateElement("hash_10FB7C42");
                    hash_10FB7C42.SetAttribute("value", "1,0");
                    itemRoot.AppendChild(hash_10FB7C42);

                    XmlElement parentIndex = YMAPxml.CreateElement("parentIndex");
                    parentIndex.SetAttribute("value", "" + float.Parse(entry[9]));
                    itemRoot.AppendChild(parentIndex);

                    XmlElement lodDist = YMAPxml.CreateElement("lodDist");
                    lodDist.SetAttribute("value", "-1,0");
                    itemRoot.AppendChild(lodDist);

                    XmlElement hash_CA974BCD = YMAPxml.CreateElement("hash_CA974BCD");
                    hash_CA974BCD.SetAttribute("value", "0,0");
                    itemRoot.AppendChild(hash_CA974BCD);

                    XmlElement hash_6C8F1715 = YMAPxml.CreateElement("hash_6C8F1715");
                    hash_6C8F1715.AppendChild(YMAPxml.CreateTextNode("enum_hash_7D934F41"));
                    itemRoot.AppendChild(hash_6C8F1715);

                    XmlElement hash_A687AC89 = YMAPxml.CreateElement("hash_A687AC89");
                    hash_A687AC89.SetAttribute("value", "0");
                    itemRoot.AppendChild(hash_A687AC89);

                    XmlElement hash_2691F019 = YMAPxml.CreateElement("hash_2691F019");
                    hash_2691F019.AppendChild(YMAPxml.CreateTextNode("enum_hash_73D556CB"));
                    itemRoot.AppendChild(hash_2691F019);

                    XmlElement extensions = YMAPxml.CreateElement("extensions");
                    itemRoot.AppendChild(extensions);

                    XmlElement hash_18C1D587 = YMAPxml.CreateElement("hash_18C1D587");
                    hash_18C1D587.SetAttribute("value", "255");
                    itemRoot.AppendChild(hash_18C1D587);

                    XmlElement hash_23C0E543 = YMAPxml.CreateElement("hash_23C0E543");
                    hash_23C0E543.SetAttribute("value", "255");
                    itemRoot.AppendChild(hash_23C0E543);

                    XmlElement hash_3C852527 = YMAPxml.CreateElement("hash_3C852527");
                    hash_3C852527.SetAttribute("value", "0");
                    itemRoot.AppendChild(hash_3C852527);
                }

                YMAPxml.Save(ideFile.ToString().Replace(".ide", ".YMAP.xml"));

                DialogResult dialog = MessageBox.Show("Finished");
            }
        }

        private void YTYP_Button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog FBD = new FolderBrowserDialog();

            if (FBD.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            var ideMapping = new Dictionary<string, string[]>();
            var sourceIde = Directory.GetFiles(FBD.SelectedPath, "*.ide");

            foreach (var ideFile in sourceIde)
            {
                // read the source IDE
                var lines = File.ReadAllLines(ideFile);
                var inObjs = false;

                foreach (var line in lines)
                {
                    if (line == "objs")
                    {
                        inObjs = true;
                    }
                    else if (inObjs)
                    {
                        if (line == "end")
                        {
                            break;
                        }

                        var bits = line.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

                        if (bits.Last() == "null")
                        {
                            ideMapping[bits[0].ToLower()] = bits;
                        }
                    }
                }

                // write the object
                //Create the document
                XmlDocument YTYPxml = new XmlDocument();

                XmlNode docNode = YTYPxml.CreateXmlDeclaration("1.0", "UTF-8", null);
                YTYPxml.AppendChild(docNode);

                XmlElement CMapT = YTYPxml.CreateElement("CMapTypes");
                YTYPxml.AppendChild(CMapT);

                //Root of the xml tree
                XmlElement init = YTYPxml.CreateElement("archetypes");
                CMapT.AppendChild(init);

                //Copyright
                XmlComment newComment = YTYPxml.CreateComment("IDE&OPL2XML by indirivacua");
                YTYPxml.AppendChild(newComment);

                foreach (var entry in ideMapping)
                {
                    XmlElement itemRoot = YTYPxml.CreateElement("Item");
                    itemRoot.SetAttribute("type", "CBaseArchetypeDef");
                    init.AppendChild(itemRoot);

                    XmlElement modelName = YTYPxml.CreateElement("name");
                    modelName.AppendChild(YTYPxml.CreateTextNode(entry.Value[0].ToLower()));
                    itemRoot.AppendChild(modelName);

                    XmlElement assetName = YTYPxml.CreateElement("assetName");
                    assetName.AppendChild(YTYPxml.CreateTextNode(entry.Value[0].ToLower()));
                    itemRoot.AppendChild(assetName);

                    XmlElement texName = YTYPxml.CreateElement("textureDictionary");
                    texName.AppendChild(YTYPxml.CreateTextNode(entry.Value[1].ToLower()));
                    itemRoot.AppendChild(texName);

                    XmlElement bbMin = YTYPxml.CreateElement("bbMin");
                    bbMin.SetAttribute("x", "" + float.Parse(entry.Value[5]));
                    bbMin.SetAttribute("y", "" + float.Parse(entry.Value[6]));
                    bbMin.SetAttribute("z", "" + float.Parse(entry.Value[7]));
                    itemRoot.AppendChild(bbMin);

                    XmlElement bbMax = YTYPxml.CreateElement("bbMax");
                    bbMax.SetAttribute("x", "" + float.Parse(entry.Value[8]));
                    bbMax.SetAttribute("y", "" + float.Parse(entry.Value[9]));
                    bbMax.SetAttribute("z", "" + float.Parse(entry.Value[10]));
                    itemRoot.AppendChild(bbMax);

                    XmlElement bsCentre = YTYPxml.CreateElement("bsCentre");
                    bsCentre.SetAttribute("x", "" + float.Parse(entry.Value[11]));
                    bsCentre.SetAttribute("y", "" + float.Parse(entry.Value[12]));
                    bsCentre.SetAttribute("z", "" + float.Parse(entry.Value[13]));
                    itemRoot.AppendChild(bsCentre);

                    XmlElement bsRadius = YTYPxml.CreateElement("bsRadius");
                    bsRadius.SetAttribute("value", "" + float.Parse(entry.Value[14]));
                    itemRoot.AppendChild(bsRadius);

                    XmlElement drawableDictionary = YTYPxml.CreateElement("drawableDictionary");
                    drawableDictionary.AppendChild(YTYPxml.CreateTextNode(entry.Value[15].ToLower()));
                    itemRoot.AppendChild(drawableDictionary);

                    XmlElement lodDist = YTYPxml.CreateElement("lodDist");
                    lodDist.SetAttribute("value", "" + float.Parse(entry.Value[2]));
                    itemRoot.AppendChild(lodDist);

                    XmlElement flags = YTYPxml.CreateElement("flags");
                    flags.SetAttribute("value", "536870912");
                    itemRoot.AppendChild(flags);

                    XmlElement specialAttribute = YTYPxml.CreateElement("specialAttribute");
                    specialAttribute.SetAttribute("value", "0");//31
                    itemRoot.AppendChild(specialAttribute);

                    XmlElement hash_AD5D5B4C = YTYPxml.CreateElement("hash_AD5D5B4C");
                    hash_AD5D5B4C.SetAttribute("value", "15,0");
                    itemRoot.AppendChild(hash_AD5D5B4C);

                    XmlElement hash_19471791 = YTYPxml.CreateElement("hash_19471791");
                    itemRoot.AppendChild(hash_19471791);

                    XmlElement hash_D3C717FC = YTYPxml.CreateElement("hash_D3C717FC");
                    hash_D3C717FC.AppendChild(YTYPxml.CreateTextNode(""));//prop_palm_sm_01e
                    itemRoot.AppendChild(hash_D3C717FC);

                    XmlElement hash_069004B2 = YTYPxml.CreateElement("hash_069004B2");
                    hash_069004B2.AppendChild(YTYPxml.CreateTextNode("enum_hash_07C0CB71"));
                    itemRoot.AppendChild(hash_069004B2);

                    //New sub-tree
                    XmlElement extensions = YTYPxml.CreateElement("extensions");
                    itemRoot.AppendChild(extensions);

                    if (checkBox1.Checked == true)
                    {
                        //ParticleEffects
                        XmlElement rootItemExtension = YTYPxml.CreateElement("Item");
                        rootItemExtension.SetAttribute("type", "CExtensionDefParticleEffect");
                        extensions.AppendChild(rootItemExtension);

                        XmlElement modelName2 = YTYPxml.CreateElement("name");
                        modelName2.AppendChild(YTYPxml.CreateTextNode(""));//prop_palm_sm_01d
                        rootItemExtension.AppendChild(modelName2);

                        XmlElement offsetPosition = YTYPxml.CreateElement("offsetPosition");
                        offsetPosition.SetAttribute("x", "0,0");//0,4677734
                        offsetPosition.SetAttribute("y", "0,0");
                        offsetPosition.SetAttribute("z", "0,0");//6,720062
                        rootItemExtension.AppendChild(offsetPosition);

                        XmlElement offsetRotation = YTYPxml.CreateElement("offsetRotation");
                        offsetRotation.SetAttribute("x", "0,0");
                        offsetRotation.SetAttribute("y", "0,0");
                        offsetRotation.SetAttribute("z", "0,0");
                        offsetRotation.SetAttribute("w", "1,0");
                        rootItemExtension.AppendChild(offsetRotation);

                        XmlElement fxName = YTYPxml.CreateElement("fxName");
                        fxName.AppendChild(YTYPxml.CreateTextNode(comboBox1.Text));
                        rootItemExtension.AppendChild(fxName);

                        XmlElement boneTag = YTYPxml.CreateElement("boneTag");
                        boneTag.SetAttribute("value", "0");
                        rootItemExtension.AppendChild(boneTag);

                        XmlElement scale = YTYPxml.CreateElement("scale");
                        scale.SetAttribute("value", "1,0");
                        rootItemExtension.AppendChild(scale);

                        XmlElement probability = YTYPxml.CreateElement("probability");
                        probability.SetAttribute("value", "100");
                        rootItemExtension.AppendChild(probability);

                        XmlElement flags2 = YTYPxml.CreateElement("flags");
                        flags2.SetAttribute("value", "1");
                        rootItemExtension.AppendChild(flags2);

                        XmlElement color = YTYPxml.CreateElement("color");
                        color.SetAttribute("value", "4290891967");
                        rootItemExtension.AppendChild(color);
                    }
                }

                YTYPxml.Save(ideFile.ToString().Replace(".ide", ".YTYP.xml"));

                DialogResult dialog = MessageBox.Show("Finished");
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
                MessageBox.Show("May not work as expected.", "Unfinished feature", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                MessageBox.Show("# Thanks to CodeWalker for the code to read .ypt files. \n\n# Format: \n# [asset_name]\n# effect_name_1\n# effect_name_2", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
                comboBox1.Enabled = true;
            }
            else
            {
                comboBox1.Enabled = false;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((comboBox1.SelectedItem.ToString().IndexOf("[") != -1) || String.IsNullOrEmpty(comboBox1.SelectedItem.ToString()))
            {
                MessageBox.Show("You can't select this particle effect.", "Stop right there, pal!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                comboBox1.SelectedItem = previusSelectedParticleEffect;
            }
            previusSelectedParticleEffect = comboBox1.SelectedItem;
        }
    }
}