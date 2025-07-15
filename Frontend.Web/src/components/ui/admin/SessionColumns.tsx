"use client";

import { Badge } from "@/components/Badge";
import { Checkbox } from "@/components/Checkbox";
import { DataTableColumnHeader, DataTableRowActions } from "@/components/DataTable";
import { ChatSession } from "@/lib/definitions";
import { ColumnDef, createColumnHelper } from "@tanstack/react-table";
import { formatDistanceToNow } from "date-fns";
import { MessageSquare } from "lucide-react";

const columnHelper = createColumnHelper<ChatSession>();

export const SessionColumns = ({ onDelete }: { onDelete: (userId: string, sessionId: string) => Promise<void> }) => [
  columnHelper.display({
    id: "select",
    header: ({ table }) => (
      <Checkbox
        checked={
          table.getIsAllPageRowsSelected()
            ? true
            : table.getIsSomeRowsSelected()
              ? "indeterminate"
              : false
        }
        onCheckedChange={() => table.toggleAllPageRowsSelected()}
        className="translate-y-0.5"
        aria-label="Select all"
      />
    ),
    cell: ({ row }) => (
      <Checkbox
        checked={row.getIsSelected()}
        onCheckedChange={() => row.toggleSelected()}
        className="translate-y-0.5"
        aria-label="Select row"
      />
    ),
    enableSorting: false,
    enableHiding: false,
    meta: {
      displayName: "Select",
    },
  }),
  columnHelper.accessor("id", {
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title="Session ID" />
    ),
    enableSorting: true,
    enableHiding: false,
    meta: {
      className: "text-left",
      displayName: "Session ID",
    },
    cell: ({ getValue }) => {
      return (
        <span className="font-mono">{getValue()}</span>
      )
    },
  }),
  columnHelper.accessor("userId", {
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title="User ID" />
    ),
    enableSorting: true,
    enableHiding: true,
    meta: {
      className: "text-left",
      displayName: "User ID",
    },
    cell: ({ getValue }) => (
      <Badge variant="success" className="font-mono">
        {getValue()}
      </Badge>
    ),
  }),
  columnHelper.accessor("title", {
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title="Title" />
    ),
    enableSorting: true,
    enableHiding: false,
    meta: {
      className: "text-left",
      displayName: "Title",
    },
    cell: ({ getValue }) => (
      <span>
        {getValue() || <em className="text-gray-400">(untitled)</em>}
      </span>
    ),
  }),
  columnHelper.accessor("timestamp", {
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title="Timestamp" />
    ),
    enableSorting: true,
    enableHiding: false,
    meta: {
      className: "tabular-nums",
      displayName: "Timestamp",
    },
    cell: ({ getValue }) => (
      <span className="text-xs text-gray-500">
        {formatDistanceToNow(new Date(getValue()), { addSuffix: true })}
      </span>
    ),
  }),
  columnHelper.display({
    id: "messages",
    header: "Messages",
    enableSorting: false,
    enableHiding: true,
    meta: {
      className: "text-right",
      displayName: "Messages",
    },
    cell: ({ row }) => (
      <span className="flex items-center gap-1">
        <MessageSquare className="w-4 h-4 text-blue-500" />
        <span className="font-semibold">{row.original.messages.length}</span>
      </span>
    ),
  }),
  columnHelper.display({
    id: "edit",
    header: "Edit",
    enableSorting: false,
    enableHiding: false,
    meta: {
      className: "text-right",
      displayName: "Edit",
    },
    cell: ({ row }) => {
      const deleteSession = async () => {
        await onDelete(row.original.userId, row.original.id);
      };

      return (
        <DataTableRowActions row={row} subject={row.original.id} delete={deleteSession} />
      );
    },
  }),
] as ColumnDef<ChatSession>[];