// <copyright file="ChatHub.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Hubs
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Cosmos.BlobService;
    using Cosmos.Cms.Models;
    using Cosmos.Common.Data;
    using Cosmos.Editor.Data.Logic;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;

    /// <summary>
    /// Chat hub.
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext dbContext;
        private readonly ArticleEditLogic articleLogic;
        private readonly StorageContext storageContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatHub"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="articleLogic"></param>
        /// <param name="storageContext"></param>
        public ChatHub(ApplicationDbContext dbContext, ArticleEditLogic articleLogic, StorageContext storageContext)
        {
            this.dbContext = dbContext;
            this.articleLogic = articleLogic;
            this.storageContext = storageContext;
        }

        #region CHAT METHODS

        /// <summary>
        /// Chat send method.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task Send(object sender, string message)
        {
            // Broadcast the message to all clients except the sender
            await Clients.Others.SendAsync("broadcastMessage", sender, message);
        }

        /// <summary>
        /// Send or broadcast message.
        /// </summary>
        /// <param name="sender"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SendTyping(object sender)
        {
            // Broadcast the typing notification to all clients except the sender
            await Clients.Others.SendAsync("typing", sender);
        }

        /// <summary>
        /// Signals that the user has stopped typing in the chat window.
        /// </summary>
        /// <param name="sender"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task StopTyping(object sender)
        {
            // Broadcast the typing notification to all clients except the sender
            await Clients.Others.SendAsync("stoptyping", sender);
        }

        #endregion

        #region ARTICLE EDITING

        /// <summary>
        /// Signal other members of the group that a page was just saved, and to reload page.
        /// </summary>
        /// <param name="id">Article RECORD ID.</param>
        /// <param name="editorType"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ArticleSaved(string id, string editorType)
        {
            var result = await GetContent(id, editorType);
            await ClearLocks(id);
            await Clients.OthersInGroup(id).SendAsync("ArticleReload", result);
        }

        /// <summary>
        /// Signifies an article has been loaded, and everyone needs to refresh.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="editorType"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ArticleImported(string id, string editorType)
        {
            var result = await GetContent(id, editorType);
            await ClearLocks(id);
            await Clients.Group(id).SendAsync("ArticleReload", result);
        }

        /// <summary>
        /// Abandon edits, make everyone reload original.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="editorType"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task AbandonEdits(string id, string editorType)
        {
            var result = await GetContent(id, editorType);
            await Clients.Group(id).SendAsync("ArticleReload", result);
            await ClearLocks(id);
        }

        /// <summary>
        /// Gets content for a editor.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="editorType"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<string> GetContent(string id, string editorType)
        {
            switch (editorType)
            {
                case "FileEditor":
                    {
                        // Open a stream
                        await using var memoryStream = new MemoryStream();

                        await using (var stream = await storageContext.OpenBlobReadStreamAsync(id))
                        {
                            // Load into memory and release the blob stream right away
                            await stream.CopyToAsync(memoryStream);
                        }

                        var model = new FileManagerEditCodeViewModel()
                        {
                            Id = id, // ID is file path
                            Path = id,
                            EditorTitle = Path.GetFileName(Path.GetFileName(id)),
                            Content = Encoding.UTF8.GetString(memoryStream.ToArray()),
                            EditingField = "Content",
                            CustomButtons = new List<string>()
                        };
                        return JsonConvert.SerializeObject(model);
                    }

                case "LayoutEditor":
                    {
                        var model = await dbContext.Layouts.FindAsync(Guid.Parse(id));
                        return JsonConvert.SerializeObject(model);
                    }

                case "TemplateEditor":
                    {
                        var model = await dbContext.Templates.FindAsync(Guid.Parse(id));
                        return JsonConvert.SerializeObject(model);
                    }

                default:
                    {
                        var model = await articleLogic.GetArticleById(Guid.Parse(id), Controllers.EnumControllerName.Edit, Guid.Empty);
                        return JsonConvert.SerializeObject(model);
                    }
            }
        }

        /// <summary>
        /// Joins users to an editing "room".
        /// </summary>
        /// <param name="id">Article record number is the room name.</param>
        /// <param name="editorType"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task JoinRoom(string id, string editorType)
        {
            if (!string.IsNullOrEmpty(id))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, id);
            }
        }

        /// <summary>
        /// Notifies everyone on this editing room of any article lock.
        /// </summary>
        /// <param name="id">Article record ID.</param>
        /// <param name="editorType"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task NotifyRoomOfLock(string id, string editorType)
        {
            var idno = Guid.Parse(id);
            var model = await dbContext.ArticleLocks.Where(w => w.ArticleId == idno).Select(l => new ArticleLockViewModel()
            {
                ArticleRecordId = l.ArticleId,
                Id = l.Id,
                LockSetDateTime = l.LockSetDateTime,
                UserEmail = l.UserEmail,
                ConnectionId = Context.ConnectionId,
                EditorType = l.EditorType
            }).FirstOrDefaultAsync();
            string message = JsonConvert.SerializeObject(model);
            await Clients.Group(id).SendAsync("ArticleLock", message);
        }

        /// <summary>
        /// Removes a lock on an article.
        /// </summary>
        /// <param name="id">Group ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ClearLocks(string id)
        {
            var connectionId = Context.ConnectionId;
            var idno = Guid.Parse(id);

            var articleLocks = await dbContext.ArticleLocks.Where(
                w => w.ConnectionId == connectionId || w.ArticleId == idno).ToListAsync();

            if (articleLocks.Any())
            {
                var ids = articleLocks.Select(s => s.ArticleId);

                dbContext.ArticleLocks.RemoveRange(articleLocks);

                await dbContext.SaveChangesAsync();
            }

            string message = JsonConvert.SerializeObject(new ArticleLockViewModel());
            await Clients.Group(id).SendAsync("ArticleLock", message);
        }

        /// <summary>
        /// Announces that someone has started edting an article.
        /// </summary>
        /// <param name="id">Article RECORD ID.</param>
        /// <param name="editorType"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SetArticleLock(string id, string editorType)
        {
            if (!string.IsNullOrEmpty(id))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, id);
                var idno = Guid.Parse(id);
                var articleLock = await dbContext.ArticleLocks.FirstOrDefaultAsync(a => a.ArticleId == idno);
                if (articleLock == null)
                {
                    var identityUser = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == Context.User.Identity.Name);
                    articleLock = new ArticleLock()
                    {
                        Id = Guid.NewGuid(),
                        ArticleId = idno,
                        UserEmail = identityUser.Email,
                        LockSetDateTime = System.DateTimeOffset.UtcNow,
                        ConnectionId = Context.ConnectionId
                    };
                    dbContext.ArticleLocks.Add(articleLock);
                    await dbContext.SaveChangesAsync();
                }

                await NotifyRoomOfLock(id, editorType);
            }
        }

        #endregion

        /// <summary>
        /// Handles when a client disconnects.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var connectionId = Context.ConnectionId;
            try
            {
                var articleLocks = await dbContext.ArticleLocks.Where(w => w.ConnectionId == connectionId).ToListAsync();

                if (articleLocks.Any())
                {
                    foreach (var item in articleLocks)
                    {
                        var id = item.ArticleId;
                        await Groups.RemoveFromGroupAsync(connectionId, item.ArticleId.ToString());
                        dbContext.ArticleLocks.RemoveRange(articleLocks);
                        await dbContext.SaveChangesAsync();
                        await NotifyRoomOfLock(id.ToString(), item.EditorType);
                    }
                }
            }
            catch (Exception ex)
            {
                var t = ex; // for debugging
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
