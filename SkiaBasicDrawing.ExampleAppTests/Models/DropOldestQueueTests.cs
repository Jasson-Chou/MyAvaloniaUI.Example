using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaBasicDrawing.ExampleApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaBasicDrawing.ExampleApp.Models.Tests
{
    [TestClass()]
    public class DropOldestQueueTests
    {
        [TestMethod()]
        public void SetCapacityTest()
        {
            for (int capSize = 6; capSize < 10; capSize++)
            {
                int[] array = new int[capSize];
                DropOldestQueue<int> dropOldestQueue = new DropOldestQueue<int>(capSize);

                for (int i = 0; i < capSize; i++)
                {
                    dropOldestQueue.Enqueue(i);
                }

                dropOldestQueue.CopyTo(array);

                int newCapacity = capSize / 3;

                dropOldestQueue.SetCapacity(newCapacity);

                var newArray = dropOldestQueue.ToArray();

                for (int i = 0; i < newCapacity; i++)
                {
                    Assert.AreEqual(newArray[i], array[capSize - newCapacity + i]);
                }
            }
        }
    }
}