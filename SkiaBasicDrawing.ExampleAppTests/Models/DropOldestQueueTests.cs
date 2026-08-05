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
        [DataRow(true)]
        [DataRow(false)]
        public void SetCapacityToBiggerOrSmallerTest(bool biggerOrSmaller)
        {
            for (int capSize = 6; capSize < 100; capSize++)
            {
                int[] array = new int[capSize];
                for(int fillCount = 3; fillCount < capSize * 4; fillCount++)
                {
                    DropOldestQueue<int> dropOldestQueue = new DropOldestQueue<int>(capSize);
                    for (int i = 0; i < fillCount; i++)
                    {
                        dropOldestQueue.Enqueue(i);
                    }
                    
                    int actualEnqueueCount = dropOldestQueue.Count;

                    dropOldestQueue.CopyTo(array);

                    int newCapacity = biggerOrSmaller ? capSize * 2 : capSize / 3;

                    try
                    {
                        dropOldestQueue.SetCapacity(newCapacity);
                    }
                    catch
                    {
                        Assert.Fail($"capSize:{capSize}, fillCount:{fillCount}, newCapacity:{newCapacity}{Environment.NewLine}" +
                            $"array:{string.Join(",", array)}");
                    }

                    var newArray = dropOldestQueue.ToArray();

                    int expectedCount = Math.Min(actualEnqueueCount, newCapacity);

                    for (int i = 0; i < expectedCount; i++)
                    {
                        int actualArrIdx = actualEnqueueCount - expectedCount + i;
                        try
                        {
                            int newArrayValue = newArray[i];
                            int arrayValue = array[actualArrIdx];

                            Assert.AreEqual(newArrayValue, arrayValue,
                                $"capSize:{capSize}, fillCount:{fillCount}, " +
                                $"expectedCount:{expectedCount}, newCapacity:{newCapacity}, newArray:{i}{Environment.NewLine}" +
                                $"{nameof(array)}:{string.Join(",", array)}{Environment.NewLine}" +
                                $"{nameof(newArray)}:{string.Join(",", newArray)}");
                        }
                        catch(Exception e)
                        {
                            Assert.Fail($"Actual Arr Index: {actualArrIdx}{Environment.NewLine}" +
                                $"capSize:{capSize}, fillCount:{fillCount}, actualEnqueueCount:{actualEnqueueCount}, " +
                                $"expectedCount:{expectedCount}, newCapacity:{newCapacity}, newArray[{i}]{Environment.NewLine}" +
                                $"{nameof(array)}:{string.Join(",", array)}{Environment.NewLine}" +
                                $"{nameof(newArray)}:{string.Join(",", newArray)}{Environment.NewLine}" +
                                $"Exception: {e}");
                        }
                        
                    }
                }
            }
        }
    }
}