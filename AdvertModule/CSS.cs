using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using MXit.Messaging;
using MXit.Messaging.MessageElements;
using System.Threading;

namespace AdvertModule
{
    class CSS
    {
        public Dictionary<String, Color> clrF_ = new Dictionary<String, Color>();
        public Dictionary<String, Color> clrB_ = new Dictionary<String, Color>();

        public Dictionary<String, Color> clr
        {
            get
            {
                bool isUseBlackBG = true;
                if (Thread.GetNamedDataSlot("isUseBlackBG") != null)
                {
                    isUseBlackBG = (bool)Thread.GetData(Thread.GetNamedDataSlot("isUseBlackBG"));
                }

                if (isUseBlackBG)
                    return clrF_;
                else
                    return clrB_;
            }
        }

        public Dictionary<String, TextMarkup[]> mrk = new Dictionary<String, TextMarkup[]>();

        private static volatile CSS instance;
        private static object syncRoot = new Object();

        private CSS()
        {

        }

        public static CSS Ins
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new CSS();

                        instance.setColorsForBlackBackground();
                        instance.setColorsForWhiteBackground();
                    }
                }

                return instance;
            }
        }

        public void setColorsForBlackBackground()
        {
            mrk["d"] = new TextMarkup[] { };
            mrk["i"] = new TextMarkup[] { TextMarkup.Italics };
            mrk["b"] = new TextMarkup[] { TextMarkup.Bold };

            clrF_["light"] = AdvertConfig.lightColor_forBlackBG;
        }

        public void setColorsForWhiteBackground()
        {
            mrk["d"] = new TextMarkup[] { };
            mrk["i"] = new TextMarkup[] { TextMarkup.Italics };
            mrk["b"] = new TextMarkup[] { TextMarkup.Bold };

            clrB_["light"] = AdvertConfig.lightColor_forWhiteBG;
        }


    }
}
