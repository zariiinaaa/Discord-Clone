export enum ServerPermission {
  ViewChannels = 1,
  ManageChannels = 2,
  ManageServer = 3,
  ManageRoles = 4,

  CreateInvites = 5,
  KickMembers = 6,
  BanMembers = 7,
  ModerateMembers = 8,

  ChangeNickname = 9,
  ManageNicknames = 10,

  SendMessages = 11,
  ManageMessages = 12,
  ReadMessageHistory = 13,
  AddReactions = 14,

  EmbedLinks = 15,
  AttachFiles = 16,
  MentionEveryone = 17,

  Connect = 18,
  Speak = 19,
  MuteMembers = 20,
  DeafenMembers = 21,
  MoveMembers = 22,

  ManageExpressions = 23,
  ViewAuditLog = 24,
  Administrator = 25,
}

export interface ServerPermissionOption {
  value: ServerPermission;
  label: string;
  category: string;
}

export const SERVER_PERMISSION_OPTIONS: ServerPermissionOption[] = [
  {
    value: ServerPermission.Administrator,
    label: "Administrator",
    category: "Advanced",
  },
  {
    value: ServerPermission.ViewAuditLog,
    label: "View Audit Log",
    category: "Advanced",
  },

  {
    value: ServerPermission.ManageServer,
    label: "Manage Server",
    category: "General Server",
  },
  {
    value: ServerPermission.ManageRoles,
    label: "Manage Roles",
    category: "General Server",
  },
  {
    value: ServerPermission.ManageChannels,
    label: "Manage Channels",
    category: "General Server",
  },
  {
    value: ServerPermission.ViewChannels,
    label: "View Channels",
    category: "General Server",
  },
  {
    value: ServerPermission.CreateInvites,
    label: "Create Invites",
    category: "General Server",
  },
  {
    value: ServerPermission.ManageExpressions,
    label: "Manage Expressions",
    category: "General Server",
  },

  {
    value: ServerPermission.KickMembers,
    label: "Kick Members",
    category: "Membership",
  },
  {
    value: ServerPermission.BanMembers,
    label: "Ban Members",
    category: "Membership",
  },
  {
    value: ServerPermission.ModerateMembers,
    label: "Moderate Members",
    category: "Membership",
  },
  {
    value: ServerPermission.ChangeNickname,
    label: "Change Nickname",
    category: "Membership",
  },
  {
    value: ServerPermission.ManageNicknames,
    label: "Manage Nicknames",
    category: "Membership",
  },

  {
    value: ServerPermission.SendMessages,
    label: "Send Messages",
    category: "Text Channel",
  },
  {
    value: ServerPermission.ManageMessages,
    label: "Manage Messages",
    category: "Text Channel",
  },
  {
    value: ServerPermission.ReadMessageHistory,
    label: "Read Message History",
    category: "Text Channel",
  },
  {
    value: ServerPermission.AddReactions,
    label: "Add Reactions",
    category: "Text Channel",
  },
  {
    value: ServerPermission.EmbedLinks,
    label: "Embed Links",
    category: "Text Channel",
  },
  {
    value: ServerPermission.AttachFiles,
    label: "Attach Files",
    category: "Text Channel",
  },
  {
    value: ServerPermission.MentionEveryone,
    label: "Mention Everyone",
    category: "Text Channel",
  },

  {
    value: ServerPermission.Connect,
    label: "Connect",
    category: "Voice Channel",
  },
  {
    value: ServerPermission.Speak,
    label: "Speak",
    category: "Voice Channel",
  },
  {
    value: ServerPermission.MuteMembers,
    label: "Mute Members",
    category: "Voice Channel",
  },
  {
    value: ServerPermission.DeafenMembers,
    label: "Deafen Members",
    category: "Voice Channel",
  },
  {
    value: ServerPermission.MoveMembers,
    label: "Move Members",
    category: "Voice Channel",
  },
];