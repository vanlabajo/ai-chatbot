import { Button } from "@/components/Button";
import { Dialog, DialogClose, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/Dialog";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/Dropdown";
import { RiDeleteBin5Line, RiMoreFill, RiPencilFill } from "@remixicon/react";
import { useState } from "react";

export function SidebarLinkActions({
  edit,
  delete: del,
  subject,
}: {
  edit?: () => void;
  delete?: () => void;
  subject?: string;
}) {
  const [open, setOpen] = useState(false);

  const handleDelete = () => setOpen(true);
  const handleClose = () => setOpen(false);
  const handleConfirmDelete = () => {
    del?.();
    setOpen(false);
  };

  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            className="group aspect-square p-1.5 hover:border hover:border-gray-300 data-[state=open]:border-gray-300 data-[state=open]:bg-gray-50 hover:dark:border-gray-700 data-[state=open]:dark:border-gray-700 data-[state=open]:dark:bg-gray-900"
            onClick={(e: { stopPropagation: () => any; }) => e.stopPropagation()}
          >
            <RiMoreFill
              className="size-4 shrink-0 text-gray-500 group-hover:text-gray-700 group-data-[state=open]:text-gray-700 group-hover:dark:text-gray-300 group-data-[state=open]:dark:text-gray-300"
              aria-hidden="true"
            />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="min-w-36">
          {edit && (
            <DropdownMenuItem onClick={() => setTimeout(() => edit(), 0)}>
              <span className="flex items-center gap-2">
                <RiPencilFill className="size-4 shrink-0" />
                Rename
              </span>
            </DropdownMenuItem>
          )}
          {del && (
            <DropdownMenuItem className="text-red-600 dark:text-red-500" onClick={handleDelete}>
              <span className="flex items-center gap-2">
                <RiDeleteBin5Line className="size-4 shrink-0" />
                Delete
              </span>
            </DropdownMenuItem>
          )}
        </DropdownMenuContent>
      </DropdownMenu>
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Confirm Delete</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete
              <span className="font-mono font-bold text-red-600"> {subject} </span>
              ? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <DialogClose asChild>
              <Button variant="secondary" onClick={handleClose}>Cancel</Button>
            </DialogClose>
            <Button variant="destructive" onClick={handleConfirmDelete}>Delete</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
};