// <copyright file="Retry.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.MicrosoftGraph
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    // https://stackoverflow.com/questions/1563191/cleanest-way-to-write-retry-logic

    /// <summary>
    /// Generic retry logic.
    /// </summary>
    public static class Retry
    {
        /// <summary>
        /// Executes the retry function.
        /// </summary>
        /// <param name="action">Action to retry.</param>
        /// <param name="retryInterval">Interval between retries.</param>
        /// <param name="maxAttemptCount">Maximum tries.</param>
        public static void Do(
            Action action,
            TimeSpan retryInterval,
            int maxAttemptCount = 5)
        {
            Do<object?>(
                () =>
                {
                    action();
                    return null;
                }, retryInterval,
                maxAttemptCount);
        }

        /// <summary>
        /// Retry function.
        /// </summary>
        /// <typeparam name="T">Type to return.</typeparam>
        /// <param name="action">Action to try.</param>
        /// <param name="retryInterval">Interval between tries.</param>
        /// <param name="maxAttemptCount">Maximun tries.</param>
        /// <returns>Returns <see cref="Type"/> upon success.</returns>
        public static T Do<T>(
            Func<T> action,
            TimeSpan retryInterval,
            int maxAttemptCount = 5)
        {
            var exceptions = new List<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                    {
                        Thread.Sleep(retryInterval);
                    }

                    return action();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            throw new AggregateException(exceptions);
        }
    }
}
