﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace xAPI.Utils
{
    abstract class CustomTag
    {
        private static int lastTag;
        private static int maxTag = 1000000;

        private static Object locker = new Object();

        /// <summary>
        /// Return next custom tag.
        /// </summary>
        /// <returns>Next custom tag</returns>
        public static string Next()
        {
            lock (locker)
            {
                lastTag = ++lastTag % maxTag;
                return lastTag.ToString(CultureInfo.InvariantCulture);
            }
        }

    }
}
