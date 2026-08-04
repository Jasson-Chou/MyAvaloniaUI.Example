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
            for (int capSize = 6; capSize < 100; capSize++)
            {
                int[] array = new int[capSize];
                DropOldestQueue<int> smallpart_DropOldestQueue = new DropOldestQueue<int>(capSize);
                DropOldestQueue<int> bigpart_DropOldestQueue = new DropOldestQueue<int>(capSize);
                int addCount = capSize - 1;
                for (int i = 0; i < addCount; i++)
                {
                    smallpart_DropOldestQueue.Enqueue(i);
                    bigpart_DropOldestQueue.Enqueue(i);
                }

                smallpart_DropOldestQueue.CopyTo(array);

                int newSmallCapacity = capSize / 3;
                int newBigCapacity = capSize * 3;

                smallpart_DropOldestQueue.SetCapacity(newSmallCapacity);
                bigpart_DropOldestQueue.SetCapacity(newBigCapacity);

                var small_newArray = smallpart_DropOldestQueue.ToArray();

                int expectedCount = Math.Min(capSize, newSmallCapacity);

                for (int i = 0; i < expectedCount; i++)
                {
                    Assert.AreEqual(small_newArray[i], array[addCount - expectedCount + i]);
                }
            }
        }
    }
}