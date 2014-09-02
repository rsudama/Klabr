
using System;
using System.Collections.Generic;
using System.IO;
using Gemcom.Klabr.SPWebLib;

namespace Gemcom.Klabr
{
    public class SubscribedDiscussions : List<SPDiscussion>
    {
        private readonly string _filename;


        public SubscribedDiscussions(string filename)
        {
            _filename = filename;

            if (File.Exists(_filename))
            {
                FileStream reader = new FileStream(MainWindow.SubscribedDiscussionsFile, FileMode.Open);
                AddRange((List<SPDiscussion>)MainWindow.DiscussionSerializer.Deserialize(reader));
                reader.Close();
            }
        }

        public new void Add(SPDiscussion discussion)
        {
            Add(discussion, true);
        }

        public void Add(SPDiscussion discussion, bool save)
        {
            SPDiscussion existingDiscussion = FindDiscussion(discussion.UniqueID);
            if (existingDiscussion != null)
            {
                foreach (SPTopic topic in discussion.Topics)
                {
                    SPTopic existingTopic = FindTopic(existingDiscussion.UniqueID, topic.UniqueID);
                    if (existingTopic != null)
                    {
                        topic.IsSubscribed = existingTopic.IsSubscribed;
                        if (topic.LastViewed < existingTopic.LastViewed)
                            topic.LastViewed = existingTopic.LastViewed;
                    }
                }
                discussion.LastViewed = existingDiscussion.LastViewed;
                Remove(existingDiscussion);
            }
            base.Add(discussion);
            if (save)
                Save();
        }

        public new void Remove(SPDiscussion discussion)
        {
            Remove(discussion, true);
        }

        private void Remove(SPDiscussion discussion, bool save)
        {
            SPDiscussion existingDiscussion = FindDiscussion(discussion.UniqueID);

            if (existingDiscussion == null) 
                return;

            base.Remove(existingDiscussion);
            if (save)
                Save();
        }

        /// <summary>
        /// Save discussions as subscriptions.
        /// </summary>
        /// <returns>true if save completed successfully</returns>
        public bool Save()
        {
            Stream writer = null;

            try
            {
                writer = new FileStream(_filename, FileMode.Create);
                MainWindow.DiscussionSerializer.Serialize(writer, this);
                writer.Close();
                return true;
            }
            catch (Exception exception)
            {
                if (writer != null)
                    writer.Close();
                MainWindow.HandleException(exception);
            }
            return false;
        }

        /// <summary>
        /// Updates the specified discussion and topics before saving all subscriptions.
        /// </summary>
        /// <param name="discussion"></param>
        /// <returns></returns>
        public bool SaveDiscussion(SPDiscussion discussion)
        {
            SPDiscussion subscribedDiscussion = FindDiscussion(discussion.UniqueID);

            if (subscribedDiscussion != null)
            {
                subscribedDiscussion.Topics = discussion.Topics;
                subscribedDiscussion.LastViewed = DateTime.Now;
                return Save();
            }

            return false;
        }

        public SPDiscussion FindDiscussion(Guid discussionId)
        {
            return Find(d => discussionId == d.UniqueID);
        }

        //public SPTopic FindTopic(Guid topicId)
        //{
        //    foreach (SPDiscussion discussion in this)
        //    {
        //        SPTopic topic = discussion.Topics.Find(t => topicId == t.UniqueID);
        //        if (topic != null)
        //            return topic;
        //    }
        //    return null;
        //}

        public SPTopic FindTopic(Guid discussionId, Guid topicId)
        {
            SPDiscussion discussion = FindDiscussion(discussionId);

            if (discussion != null)
                return discussion.Topics.Find(t => topicId == t.UniqueID);

            return null;
        }
    }
}
