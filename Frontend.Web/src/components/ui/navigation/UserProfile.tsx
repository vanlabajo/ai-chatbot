import { Button } from "@/components/Button";
import { getUserInfo, getUserPhotoAvatar } from "@/lib/msalGraph";
import { cx, focusRing } from "@/lib/utils";
import { RiMore2Fill } from "@remixicon/react";
import Image from "next/image";
import { useEffect, useRef, useState } from "react";
import { DropdownUserProfile } from "./DropdownUserProfile";
import { LoadingStatus } from "./LoadingStatus";

function getUserInitials(displayName: string | undefined, ignoreList: string[] = ["ext"]): string | null {
  if (!displayName) return null;

  return displayName
    .split(" ")
    .filter((name) => !ignoreList.includes(name))
    .map((name) => name[0])
    .join("")
    .toUpperCase();
}

export const UserProfile = () => {
  const [userName, setUserName] = useState<string | undefined>(undefined);
  const [userInitials, setUserInitials] = useState<string | null>(null);
  const [userPhoto, setUserPhoto] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const isFetchedRef = useRef(false);

  useEffect(() => {
    const fetchGraphData = async () => {
      if (isFetchedRef.current) {
        return;
      }
      isFetchedRef.current = true;

      try {
        const userInfo = await getUserInfo();
        setUserName(userInfo?.displayName);
        setUserInitials(getUserInitials(userInfo?.displayName));

        const userPhotoUrl = await getUserPhotoAvatar();
        if (userPhotoUrl) {
          setUserPhoto(userPhotoUrl);
        }
      } catch (error) {
        console.error("Error fetching user data:", error);
        setError("Failed to load user data.");
      } finally {
        setIsLoading(false);
      }
    };

    fetchGraphData();
  }, []);

  if (isLoading) {
    return (
      <LoadingStatus />
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center">
        <span className="text-red-500">{error}</span>
      </div>
    );
  }

  return (
    <DropdownUserProfile>
      <Button
        aria-label="User settings"
        variant="ghost"
        className={cx(
          focusRing,
          "group flex w-full items-center justify-between rounded-md p-2 text-sm font-medium text-gray-900 hover:bg-gray-100 data-[state=open]:bg-gray-100 data-[state=open]:bg-gray-400/10 hover:dark:bg-gray-400/10",
        )}
      >
        <span className="flex items-center gap-3" aria-label="User Profile">
          {userPhoto ? (
            <Image
              src={userPhoto}
              alt="User Photo"
              width={10}
              height={10}
              className="w-8 h-8 rounded-full dark:ring-2 dark:ring-gray-300"
            />
          ) : (
            <span
              className="flex size-7 shrink-0 items-center justify-center rounded-full border border-gray-300 bg-white text-xs text-gray-700 dark:border-gray-800 dark:bg-gray-950 dark:text-gray-300"
              aria-hidden="true"
            >
              {userInitials}
            </span>
          )}
          <span>{userName || <span className="italic text-gray-500">No Name Available</span>}</span>
        </span>
        <RiMore2Fill
          className="size-4 shrink-0 text-gray-500 group-hover:text-gray-700 group-hover:dark:text-gray-400"
          aria-hidden="true"
        />
      </Button>
    </DropdownUserProfile>
  );
};