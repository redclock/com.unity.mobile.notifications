using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Notifications.Android
{
    /// <summary>
    /// Current status of a scheduled notification, can be queried using CheckScheduledNotificationStatus.
    /// </summary>
    public enum NotificationStatus
    {
        /// <summary>
        /// Status of a specified notification cannot be determined. This is only supported on Android Marshmallow (6.0) and above.
        /// </summary>
        Unavailable = -1,

        /// <summary>
        /// A notification with a specified id could not be found.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// A notification with a specified id is scheduled but not yet delivered.
        /// </summary>
        Scheduled = 1,

        /// <summary>
        /// A notification with a specified id was already delivered.
        /// </summary>
        Delivered = 2,
    }

    struct NotificationManagerJni
    {
        private AndroidJavaClass klass;
        private AndroidJavaObject self;


        public NotificationManagerJni(AndroidJavaClass clazz, AndroidJavaObject obj)
        {
            klass = clazz;
            self = obj;
        }

        public AndroidJavaObject GetNotificationFromIntent(AndroidJavaObject activity, AndroidJavaObject intent)
        {
            return klass.CallStatic<AndroidJavaObject>("getNotificationFromIntent", activity, intent);
        }

        public void SetNotificationIcon(AndroidJavaObject builder, AndroidJavaObject keyName, string icon)
        {
            klass.CallStatic("setNotificationIcon", builder, keyName, icon);
        }

        public void SetNotificationColor(AndroidJavaObject builder, int color)
        {
            klass.CallStatic("setNotificationColor", builder, color);
        }

        public Color? GetNotificationColor(AndroidJavaObject notification)
        {
            using (var color = klass.CallStatic<AndroidJavaObject>("getNotificationColor", notification))
            {
                if (color == null)
                    return null;
                return color.Call<int>("intValue").ToColor();
            }
        }

        public void SetNotificationUsesChronometer(AndroidJavaObject builder, bool usesStopwatch)
        {
            klass.CallStatic("setNotificationUsesChronometer", builder, usesStopwatch);
        }

        public void SetNotificationGroupAlertBehavior(AndroidJavaObject builder, int groupAlertBehaviour)
        {
            klass.CallStatic("setNotificationGroupAlertBehavior", builder, groupAlertBehaviour);
        }

        public int GetNotificationGroupAlertBehavior(AndroidJavaObject notification)
        {
            return klass.CallStatic<int>("getNotificationGroupAlertBehavior", notification);
        }

        public string GetNotificationChannelId(AndroidJavaObject notification)
        {
            return klass.CallStatic<string>("getNotificationChannelId", notification);
        }

        public void RegisterNotificationChannel(AndroidNotificationChannel channel)
        {
            self.Call("registerNotificationChannel",
                channel.Id,
                channel.Name,
                (int)channel.Importance,
                channel.Description,
                channel.EnableLights,
                channel.EnableVibration,
                channel.CanBypassDnd,
                channel.CanShowBadge,
                channel.VibrationPattern,
                (int)channel.LockScreenVisibility
            );
        }

        public AndroidJavaObject[] GetNotificationChannels()
        {
            return self.Call<AndroidJavaObject[]>("getNotificationChannels");
        }

        public void DeleteNotificationChannel(string channelId)
        {
            self.Call("deleteNotificationChannel", channelId);
        }

        public void ScheduleNotification(AndroidJavaObject notificationBuilder)
        {
            self.Call("scheduleNotification", notificationBuilder);
        }

        public bool CheckIfPendingNotificationIsRegistered(int id)
        {
            return self.Call<bool>("checkIfPendingNotificationIsRegistered", id);
        }

        public void CancelPendingNotification(int id)
        {
            self.Call("cancelPendingNotification", id);
        }

        public void CancelDisplayedNotification(int id)
        {
            self.Call("cancelDisplayedNotification", id);
        }

        public void CancelAllPendingNotificationIntents()
        {
            self.Call("cancelAllPendingNotificationIntents");
        }

        public void CancelAllNotifications()
        {
            self.Call("cancelAllNotifications");
        }

        public int CheckNotificationStatus(int id)
        {
            return self.Call<int>("checkNotificationStatus", id);
        }

        public void ShowNotificationSettings(string channelId)
        {
            self.Call("showNotificationSettings", channelId);
        }

        public AndroidJavaObject CreateNotificationBuilder(String channelId)
        {
            return self.Call<AndroidJavaObject>("createNotificationBuilder", channelId);
        }
    }

    struct JniApi
    {
        public NotificationManagerJni NotificationManager;

        public static class NotificationBuilder
        {
            public static void SetContentTitle(AndroidJavaObject builder, string title)
            {
                builder.Call<AndroidJavaObject>("setContentTitle", title).Dispose();
            }

            public static void SetContentText(AndroidJavaObject builder, string text)
            {
                builder.Call<AndroidJavaObject>("setContentText", text).Dispose();
            }

            public static void SetAutoCancel(AndroidJavaObject builder, bool shouldAutoCancel)
            {
                builder.Call<AndroidJavaObject>("setAutoCancel", shouldAutoCancel).Dispose();
            }

            public static void SetNumber(AndroidJavaObject builder, int number)
            {
                builder.Call<AndroidJavaObject>("setNumber", number).Dispose();
            }

            public static void SetStyle(AndroidJavaObject builder, AndroidJavaObject style)
            {
                builder.Call<AndroidJavaObject>("setStyle", style).Dispose();
            }

            public static void SetWhen(AndroidJavaObject builder, long timestamp)
            {
                builder.Call<AndroidJavaObject>("setWhen", timestamp).Dispose();
            }

            public static void SetGroup(AndroidJavaObject builder, string group)
            {
                builder.Call<AndroidJavaObject>("setGroup", group).Dispose();
            }

            public static void SetGroupSummary(AndroidJavaObject builder, bool groupSummary)
            {
                builder.Call<AndroidJavaObject>("setGroupSummary", groupSummary).Dispose();
            }

            public static void SetSortKey(AndroidJavaObject builder, string sortKey)
            {
                builder.Call<AndroidJavaObject>("setSortKey", sortKey).Dispose();
            }

            public static void SetShowWhen(AndroidJavaObject builder, bool showTimestamp)
            {
                builder.Call<AndroidJavaObject>("setShowWhen", showTimestamp).Dispose();
            }
        }

        public static class Bundle
        {
            public static void PutInt(AndroidJavaObject bundle, AndroidJavaObject key, int value)
            {
                bundle.Call("putInt", key, value);
            }

            public static void PutLong(AndroidJavaObject bundle, AndroidJavaObject key, long value)
            {
                bundle.Call("putLong", key, value);
            }

            public static void PutString(AndroidJavaObject bundle, AndroidJavaObject key, string value)
            {
                bundle.Call("putString", key, value);
            }
        }
    }

    /// <summary>
    /// Use the AndroidNotificationCenter to register notification channels and schedule local notifications.
    /// </summary>
    public class AndroidNotificationCenter
    {
        /// <summary>
        /// The delegate type for the notification received callbacks.
        /// </summary>
        public delegate void NotificationReceivedCallback(AndroidNotificationIntentData data);

        /// <summary>
        /// Subscribe to this event to receive callbacks whenever a scheduled notification is shown to the user.
        /// </summary>
        public static event NotificationReceivedCallback OnNotificationReceived = delegate { };

        private static AndroidJavaObject s_CurrentActivity;
        private static JniApi s_Jni;
        private static bool s_Initialized = false;

        private static AndroidJavaObject Notification_EXTRA_TITLE;
        private static AndroidJavaObject Notification_EXTRA_TEXT;
        private static AndroidJavaObject Notification_EXTRA_SHOW_CHRONOMETER;
        private static AndroidJavaObject Notification_EXTRA_BIG_TEXT;
        private static AndroidJavaObject Notification_EXTRA_SHOW_WHEN;
        private static int Notification_FLAG_AUTO_CANCEL;
        private static int Notification_FLAG_GROUP_SUMMARY;

        private static AndroidJavaObject KEY_FIRE_TIME;
        private static AndroidJavaObject KEY_ID;
        private static AndroidJavaObject KEY_INTENT_DATA;
        private static AndroidJavaObject KEY_LARGE_ICON;
        private static AndroidJavaObject KEY_REPEAT_INTERVAL;
        private static AndroidJavaObject KEY_NOTIFICATION;
        private static AndroidJavaObject KEY_SMALL_ICON;

        /// <summary>
        /// Initialize the AndroidNotificationCenter class.
        /// Can be safely called multiple times
        /// </summary>
        /// <returns>True if has been successfully initialized</returns>
        public static bool Initialize()
        {
            if (s_Initialized)
                return true;

            if (AndroidReceivedNotificationMainThreadDispatcher.GetInstance() == null)
            {
                var receivedNotificationDispatcher = new GameObject("AndroidReceivedNotificationMainThreadDispatcher");
                receivedNotificationDispatcher.AddComponent<AndroidReceivedNotificationMainThreadDispatcher>();
            }

#if UNITY_EDITOR || !UNITY_ANDROID
            s_CurrentActivity = null;
#elif UNITY_ANDROID
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            s_CurrentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var context = s_CurrentActivity.Call<AndroidJavaObject>("getApplicationContext");

            var notificationManagerClass = new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationManager");
            var notificationManager = notificationManagerClass.CallStatic<AndroidJavaObject>("getNotificationManagerImpl", context, s_CurrentActivity);
            notificationManager.Call("setNotificationCallback", new NotificationCallback());
            s_Jni.NotificationManager = new NotificationManagerJni(notificationManagerClass, notificationManager);

            using (var notificationClass = new AndroidJavaClass("android.app.Notification"))
            {
                Notification_EXTRA_TITLE = notificationClass.GetStatic<AndroidJavaObject>("EXTRA_TITLE");
                Notification_EXTRA_TEXT = notificationClass.GetStatic<AndroidJavaObject>("EXTRA_TEXT");
                Notification_EXTRA_SHOW_CHRONOMETER = notificationClass.GetStatic<AndroidJavaObject>("EXTRA_SHOW_CHRONOMETER");
                Notification_EXTRA_BIG_TEXT = notificationClass.GetStatic<AndroidJavaObject>("EXTRA_BIG_TEXT");
                Notification_EXTRA_SHOW_WHEN = notificationClass.GetStatic<AndroidJavaObject>("EXTRA_SHOW_WHEN");
                Notification_FLAG_AUTO_CANCEL = notificationClass.GetStatic<int>("FLAG_AUTO_CANCEL");
                Notification_FLAG_GROUP_SUMMARY = notificationClass.GetStatic<int>("FLAG_GROUP_SUMMARY");
            }

            KEY_FIRE_TIME = notificationManagerClass.GetStatic<AndroidJavaObject>("KEY_FIRE_TIME");
            KEY_ID = notificationManagerClass.GetStatic<AndroidJavaObject>("KEY_ID");
            KEY_INTENT_DATA = notificationManagerClass.GetStatic<AndroidJavaObject>("KEY_INTENT_DATA");
            KEY_LARGE_ICON = notificationManagerClass.GetStatic<AndroidJavaObject>("KEY_LARGE_ICON");
            KEY_REPEAT_INTERVAL = notificationManagerClass.GetStatic<AndroidJavaObject>("KEY_REPEAT_INTERVAL");
            KEY_NOTIFICATION = notificationManagerClass.GetStatic<AndroidJavaObject>("KEY_NOTIFICATION");
            KEY_SMALL_ICON = notificationManagerClass.GetStatic<AndroidJavaObject>("KEY_SMALL_ICON");

            s_Initialized = true;
#endif
            return s_Initialized;
        }

        /// <summary>
        ///  Creates a notification channel that notifications can be posted to.
        ///  Notification channel settings can be changed by users on devices running Android 8.0 and above.
        ///  On older Android versions settings set on the notification channel struct will still be applied to the notification
        ///  if they are supported to by the Android version the app is running on.
        /// </summary>
        /// <param name="channel">Channel parameters</param>
        /// <remarks>
        ///  When a channel is deleted and recreated, all of the previous settings are restored. In order to change any settings
        ///  besides the name or description an entirely new channel (with a different channel ID) must be created.
        /// </remarks>
        public static void RegisterNotificationChannel(AndroidNotificationChannel channel)
        {
            if (!Initialize())
                return;

            if (string.IsNullOrEmpty(channel.Id))
            {
                throw new Exception("Cannot register notification channel, the channel ID is not specified.");
            }
            if (string.IsNullOrEmpty(channel.Name))
            {
                throw new Exception(string.Format("Cannot register notification channel: {0} , the channel Name is not set.", channel.Id));
            }
            if (string.IsNullOrEmpty(channel.Description))
            {
                throw new Exception(string.Format("Cannot register notification channel: {0} , the channel Description is not set.", channel.Id));
            }

            s_Jni.NotificationManager.RegisterNotificationChannel(channel);
        }

        /// <summary>
        /// Returns the notification channel with the specified id.
        /// The notification channel struct fields might not be identical to the channel struct used to initially register the channel if they were changed by the user.
        /// </summary>
        /// <param name="channelId">ID of the channel to retrieve</param>
        /// <returns>Channel with given ID or empty struct if such channel does not exist</returns>
        public static AndroidNotificationChannel GetNotificationChannel(string channelId)
        {
            return GetNotificationChannels().SingleOrDefault(channel => channel.Id == channelId);
        }

        /// <summary>
        /// Returns all notification channels that were created by the app.
        /// </summary>
        /// <returns>All existing channels</returns>
        public static AndroidNotificationChannel[] GetNotificationChannels()
        {
            if (!Initialize())
                return new AndroidNotificationChannel[0];

            var androidChannels = s_Jni.NotificationManager.GetNotificationChannels();
            var channels = new AndroidNotificationChannel[androidChannels == null ? 0 : androidChannels.Length];

            for (int i = 0; i < androidChannels.Length; ++i)
            {
                var channel = androidChannels[i];
                var ch = new AndroidNotificationChannel();
                ch.Id = channel.Get<string>("id");
                ch.Name = channel.Get<string>("name");
                ch.Importance = channel.Get<int>("importance").ToImportance();
                ch.Description = channel.Get<string>("description");
                ch.EnableLights = channel.Get<bool>("enableLights");
                ch.EnableVibration = channel.Get<bool>("enableVibration");
                ch.CanBypassDnd = channel.Get<bool>("canBypassDnd");
                ch.CanShowBadge = channel.Get<bool>("canShowBadge");
                ch.VibrationPattern = channel.Get<long[]>("vibrationPattern");
                ch.LockScreenVisibility = channel.Get<int>("lockscreenVisibility").ToLockScreenVisibility();

                channels[i] = ch;
            }

            return channels;
        }

        /// <summary>
        /// Delete the specified notification channel.
        /// </summary>
        /// <param name="channelId">ID of the channel to delete</param>
        public static void DeleteNotificationChannel(string channelId)
        {
            if (Initialize())
                s_Jni.NotificationManager.DeleteNotificationChannel(channelId);
        }

        /// <summary>
        /// Schedule a notification which will be shown at the time specified in the notification struct.
        /// The returned id can later be used to update the notification before it's triggered, it's current status can be tracked using CheckScheduledNotificationStatus.
        /// </summary>
        /// <param name="notification">Data for the notification</param>
        /// <param name="channelId">ID of the channel to send notification to</param>
        /// <returns>The generated ID for the notification</returns>
        public static int SendNotification(AndroidNotification notification, string channelId)
        {
            if (!Initialize())
                return -1;

            // Now.ToString("yyMMddHHmmssffffff"), but avoiding any culture-related formatting or dependencies
            var now = DateTime.UtcNow;
            var nowFormatted = $"{now.Year}{now.Month}{now.Day}{now.Hour}{now.Minute}{now.Second}{now.Millisecond}";
            int id = Math.Abs(nowFormatted.GetHashCode()) + (new System.Random().Next(10000));
            using (var builder = CreateNotificationBuilder(id, notification, channelId))
                SendNotification(builder);

            return id;
        }

        /// <summary>
        /// Schedule a notification which will be shown at the time specified in the notification struct.
        /// The specified id can later be used to update the notification before it's triggered, it's current status can be tracked using CheckScheduledNotificationStatus.
        /// </summary>
        /// <param name="notification">Data for the notification</param>
        /// <param name="channelId">ID of the channel to send notification to</param>
        /// <param name="id">A unique ID for the notification</param>
        public static void SendNotificationWithExplicitID(AndroidNotification notification, string channelId, int id)
        {
            if (Initialize())
                using (var builder = CreateNotificationBuilder(id, notification, channelId))
                {
                    SendNotification(builder);
                }
        }

        /// <summary>
        /// Schedule a notification created using the provided Notification.Builder object.
        /// Notification builder should be created by calling CreateNotificationBuilder.
        /// </summary>
        public static void SendNotification(AndroidJavaObject notificationBuilder)
        {
            if (Initialize())
                s_Jni.NotificationManager.ScheduleNotification(notificationBuilder);
        }

        /// <summary>
        /// Update an already scheduled notification.
        /// If a notification with the specified id was already scheduled it will be overridden with the information from the passed notification struct.
        /// </summary>
        /// <param name="id">ID of the notification to update</param>
        /// <param name="notification">Data for the notification</param>
        /// <param name="channelId">ID of the channel to send notification to</param>
        public static void UpdateScheduledNotification(int id, AndroidNotification notification, string channelId)
        {
            if (!Initialize())
                return;

            if (s_Jni.NotificationManager.CheckIfPendingNotificationIsRegistered(id))
            {
                using (var builder = CreateNotificationBuilder(id, notification, channelId))
                {
                    SendNotification(builder);
                }
            }
        }

        /// <summary>
        /// Cancel a scheduled or previously shown notification.
        /// The notification will no longer be displayed on it's scheduled time. If it's already delivered it will be removed from the status bar.
        /// </summary>
        /// <param name="id">ID of the notification to cancel</param>
        public static void CancelNotification(int id)
        {
            if (!Initialize())
                return;

            CancelScheduledNotification(id);
            CancelDisplayedNotification(id);
        }

        /// <summary>
        /// Cancel a scheduled notification.
        /// The notification will no longer be displayed on it's scheduled time. It it will not be removed from the status bar if it's already delivered.
        /// </summary>
        /// <param name="id">ID of the notification to cancel</param>
        public static void CancelScheduledNotification(int id)
        {
            if (Initialize())
                s_Jni.NotificationManager.CancelPendingNotification(id);
        }

        /// <summary>
        /// Cancel a previously shown notification.
        /// The notification will be removed from the status bar.
        /// </summary>
        /// <param name="id">ID of the notification to cancel</param>
        public static void CancelDisplayedNotification(int id)
        {
            if (Initialize())
                s_Jni.NotificationManager.CancelDisplayedNotification(id);
        }

        /// <summary>
        /// Cancel all notifications scheduled or previously shown by the app.
        /// All scheduled notifications will be canceled. All notifications shown by the app will be removed from the status bar.
        /// </summary>
        public static void CancelAllNotifications()
        {
            if (!Initialize())
                return;

            CancelAllScheduledNotifications();
            CancelAllDisplayedNotifications();
        }

        /// <summary>
        /// Cancel all notifications scheduled by the app.
        /// All scheduled notifications will be canceled. Notifications will not be removed from the status bar if they are already shown.
        /// </summary>
        public static void CancelAllScheduledNotifications()
        {
            if (Initialize())
                s_Jni.NotificationManager.CancelAllPendingNotificationIntents();
        }

        /// <summary>
        /// Cancel all previously shown notifications.
        /// All notifications shown by the app will be removed from the status bar. All scheduled notifications will still be shown on their scheduled time.
        /// </summary>
        public static void CancelAllDisplayedNotifications()
        {
            if (Initialize())
                s_Jni.NotificationManager.CancelAllNotifications();
        }

        /// <summary>
        /// Return the status of a scheduled notification.
        /// Only available in API  23 and above.
        /// </summary>
        /// <param name="id">ID of the notification to check</param>
        /// <returns>The status of the notification</returns>
        public static NotificationStatus CheckScheduledNotificationStatus(int id)
        {
            if (!Initialize())
                return NotificationStatus.Unavailable;

            var status = s_Jni.NotificationManager.CheckNotificationStatus(id);
            return (NotificationStatus)status;
        }

        /// <summary>
        /// Allows retrieving the notification used to open the app. You can save arbitrary string data in the 'AndroidNotification.IntentData' field.
        /// </summary>
        /// <returns>
        /// Returns the AndroidNotification used to open the app, returns null if the app was not opened with a notification.
        /// </returns>
        public static AndroidNotificationIntentData GetLastNotificationIntent()
        {
            if (!Initialize())
                return null;

            var intent = s_CurrentActivity.Call<AndroidJavaObject>("getIntent");
            var notification = s_Jni.NotificationManager.GetNotificationFromIntent(s_CurrentActivity, intent);
            if (notification == null)
                return null;
            return GetNotificationData(notification);
        }

        /// <summary>
        /// Opens settings.
        /// On Android versions lower than 8.0 opens settings for the application.
        /// On Android 8.0 and later opens notification settings for the specified channel, or for the application, if channelId is null.
        /// Note, that opening settings will suspend the application and switch to settings app.
        /// </summary>
        /// <param name="channelId">ID for the channel to open or null to open notification settings for the application.</param>
        public static void OpenNotificationSettings(string channelId = null)
        {
            if (!Initialize())
                return;

            s_Jni.NotificationManager.ShowNotificationSettings(channelId);
        }

        /// <summary>
        /// Create Notification.Builder.
        /// Will automatically generate the ID for notification.
        /// <see cref="CreateNotificationBuilder(int, AndroidNotification, string)"/>
        /// </summary>
        public static AndroidJavaObject CreateNotificationBuilder(AndroidNotification notification, string channelId)
        {
            int id = Math.Abs(DateTime.Now.ToString("yyMMddHHmmssffffff").GetHashCode()) + (new System.Random().Next(10000));
            return CreateNotificationBuilder(id, notification, channelId);
        }

        /// <summary>
        /// Create Notification.Builder object on Java side using privided AndroidNotification.
        /// </summary>
        /// <param name="id">ID for the notification</param>
        /// <param name="notification">Struct with notification data</param>
        /// <param name="channelId">Channel id</param>
        /// <returns>A proxy object for created Notification.Builder</returns>
        public static AndroidJavaObject CreateNotificationBuilder(int id, AndroidNotification notification, string channelId)
        {
            long fireTime = notification.FireTime.ToLong();
            if (fireTime < 0L)
            {
                Debug.LogError("Failed to schedule notification, it did not contain a valid FireTime");
            }

            var notificationBuilder = s_Jni.NotificationManager.CreateNotificationBuilder(channelId);
            s_Jni.NotificationManager.SetNotificationIcon(notificationBuilder, KEY_SMALL_ICON, notification.SmallIcon);
            if (!string.IsNullOrEmpty(notification.LargeIcon))
                s_Jni.NotificationManager.SetNotificationIcon(notificationBuilder, KEY_LARGE_ICON, notification.LargeIcon);
            JniApi.NotificationBuilder.SetContentTitle(notificationBuilder, notification.Title);
            JniApi.NotificationBuilder.SetContentText(notificationBuilder, notification.Text);
            JniApi.NotificationBuilder.SetAutoCancel(notificationBuilder, notification.ShouldAutoCancel);
            if (notification.Number >= 0)
                JniApi.NotificationBuilder.SetNumber(notificationBuilder, notification.Number);
            if (notification.Style == NotificationStyle.BigTextStyle)
            {
                using (var style = new AndroidJavaObject("android.app.Notification$BigTextStyle"))
                {
                    style.Call<AndroidJavaObject>("bigText", notification.Text).Dispose();
                    JniApi.NotificationBuilder.SetStyle(notificationBuilder, style);
                }
            }
            long timestampValue = notification.ShowCustomTimestamp ? notification.CustomTimestamp.ToLong() : fireTime;
            JniApi.NotificationBuilder.SetWhen(notificationBuilder, timestampValue);
            if (!string.IsNullOrEmpty(notification.Group))
                JniApi.NotificationBuilder.SetGroup(notificationBuilder, notification.Group);
            if (notification.GroupSummary)
                JniApi.NotificationBuilder.SetGroupSummary(notificationBuilder, notification.GroupSummary);
            if (!string.IsNullOrEmpty(notification.SortKey))
                JniApi.NotificationBuilder.SetSortKey(notificationBuilder, notification.SortKey);
            JniApi.NotificationBuilder.SetShowWhen(notificationBuilder, notification.ShowTimestamp);
            int color = notification.Color.ToInt();
            if (color != 0)
                s_Jni.NotificationManager.SetNotificationColor(notificationBuilder, color);
            s_Jni.NotificationManager.SetNotificationUsesChronometer(notificationBuilder, notification.UsesStopwatch);
            s_Jni.NotificationManager.SetNotificationGroupAlertBehavior(notificationBuilder, (int)notification.GroupAlertBehaviour);

            using (var extras = notificationBuilder.Call<AndroidJavaObject>("getExtras"))
            {
                JniApi.Bundle.PutInt(extras, KEY_ID, id);
                JniApi.Bundle.PutLong(extras, KEY_REPEAT_INTERVAL, notification.RepeatInterval.ToLong());
                JniApi.Bundle.PutLong(extras, KEY_FIRE_TIME, fireTime);
                if (!string.IsNullOrEmpty(notification.IntentData))
                    JniApi.Bundle.PutString(extras, KEY_INTENT_DATA, notification.IntentData);
            }

            return notificationBuilder;
        }

        internal static AndroidNotificationIntentData GetNotificationData(AndroidJavaObject notificationObj)
        {
            using (var extras = notificationObj.Get<AndroidJavaObject>("extras"))
            {
                var id = extras.Call<int>("getInt", KEY_ID, -1);
                if (id == -1)
                    return null;

                var channelId = s_Jni.NotificationManager.GetNotificationChannelId(notificationObj);
                int flags = notificationObj.Get<int>("flags");

                var notification = new AndroidNotification();
                notification.Title = extras.Call<string>("getString", Notification_EXTRA_TITLE);
                notification.Text = extras.Call<string>("getString", Notification_EXTRA_TEXT);
                notification.SmallIcon = extras.Call<string>("getString", KEY_SMALL_ICON);
                notification.LargeIcon = extras.Call<string>("getString", KEY_LARGE_ICON);
                notification.ShouldAutoCancel = 0 != (flags & Notification_FLAG_AUTO_CANCEL);
                notification.UsesStopwatch = extras.Call<bool>("getBoolean", Notification_EXTRA_SHOW_CHRONOMETER, false);
                notification.FireTime = extras.Call<long>("getLong", KEY_FIRE_TIME, -1L).ToDatetime();
                notification.RepeatInterval = extras.Call<long>("getLong", KEY_REPEAT_INTERVAL, -1L).ToTimeSpan();

                if (extras.Call<bool>("containsKey", Notification_EXTRA_BIG_TEXT))
                    notification.Style = NotificationStyle.BigTextStyle;
                else
                    notification.Style = NotificationStyle.None;

                notification.Color = s_Jni.NotificationManager.GetNotificationColor(notificationObj);
                notification.Number = notificationObj.Get<int>("number");
                notification.IntentData = extras.Call<string>("getString", KEY_INTENT_DATA);
                notification.Group = notificationObj.Call<string>("getGroup");
                notification.GroupSummary = 0 != (flags & Notification_FLAG_GROUP_SUMMARY);
                notification.SortKey = notificationObj.Call<string>("getSortKey");
                notification.GroupAlertBehaviour = s_Jni.NotificationManager.GetNotificationGroupAlertBehavior(notificationObj).ToGroupAlertBehaviours();
                var showTimestamp = extras.Call<bool>("getBoolean", Notification_EXTRA_SHOW_WHEN, false);
                notification.ShowTimestamp = showTimestamp;
                if (showTimestamp)
                    notification.CustomTimestamp = notificationObj.Get<long>("when").ToDatetime();

                var data = new AndroidNotificationIntentData(id, channelId, notification);
                data.NativeNotification = notificationObj;
                return data;
            }
        }

        internal static void ReceivedNotificationCallback(AndroidJavaObject notification)
        {
            var data = GetNotificationData(notification);
            OnNotificationReceived(data);
        }
    }
}
