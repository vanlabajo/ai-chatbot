"use client";

import React, { ChangeEvent, useState } from "react";
import ReactDOM from "react-dom";

import { cx, focusRing } from "@/lib/utils";
import invariant from "tiny-invariant";
import { useDebouncedCallback } from "use-debounce";

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeaderCell,
  TableRow,
} from "@/components/Table";

import {
  Column,
  ColumnDef,
  flexRender,
  getCoreRowModel,
  getFacetedRowModel,
  getFacetedUniqueValues,
  getFilteredRowModel,
  getPaginationRowModel,
  getSortedRowModel,
  Row,
  RowSelectionState,
  Table as TanstackTable,
  useReactTable
} from "@tanstack/react-table";

import { Button } from "@/components/Button";
import { Checkbox } from "@/components/Checkbox";
import {
  CommandBar,
  CommandBarBar,
  CommandBarCommand,
  CommandBarSeperator,
  CommandBarValue,
} from "@/components/CommandBar";
import { Input } from "@/components/Input";
import { Label } from "@/components/Label";
import {
  Popover,
  PopoverClose,
  PopoverContent,
  PopoverTrigger,
} from "@/components/Popover";
import { Searchbar } from "@/components/Searchbar";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/Select";

import {
  RiAddLine,
  RiArrowDownSLine,
  RiArrowLeftDoubleLine,
  RiArrowLeftSLine,
  RiArrowRightDoubleLine,
  RiArrowRightSLine,
  RiArrowUpSLine,
  RiCornerDownRightLine,
  RiDownloadLine,
  RiDraggable,
  RiEqualizer2Line,
  RiMoreFill,
} from "@remixicon/react";
import { RefreshCcw } from "lucide-react";

import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/Dropdown";
import { triggerPostMoveFlash } from "@atlaskit/pragmatic-drag-and-drop-flourish/trigger-post-move-flash";
import {
  attachClosestEdge,
  extractClosestEdge,
  type Edge,
} from "@atlaskit/pragmatic-drag-and-drop-hitbox/closest-edge";
import { getReorderDestinationIndex } from "@atlaskit/pragmatic-drag-and-drop-hitbox/util/get-reorder-destination-index";
import * as liveRegion from "@atlaskit/pragmatic-drag-and-drop-live-region";
import { DropIndicator } from "@atlaskit/pragmatic-drag-and-drop-react-drop-indicator/box";
import { combine } from "@atlaskit/pragmatic-drag-and-drop/combine";
import {
  draggable,
  dropTargetForElements,
  monitorForElements,
} from "@atlaskit/pragmatic-drag-and-drop/element/adapter";
import { pointerOutsideOfPreview } from "@atlaskit/pragmatic-drag-and-drop/element/pointer-outside-of-preview";
import { setCustomNativeDragPreview } from "@atlaskit/pragmatic-drag-and-drop/element/set-custom-native-drag-preview";
import { reorder } from "@atlaskit/pragmatic-drag-and-drop/reorder";
import { Dialog, DialogClose, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "./Dialog";

interface DataTableProps<TData> {
  columns: ColumnDef<TData>[];
  data: TData[];
  columnFilters?: Record<string, string>;
  columnToSearch?: string;
  refreshData: () => void;
  exportData: () => void;
  bulkDelete: (selectedRows: TData[]) => void;
}

export const DataTable = <TData,>({ columns, data, columnFilters, columnToSearch, refreshData, exportData, bulkDelete }: DataTableProps<TData>) => {
  const pageSize = 20;
  const [rowSelection, setRowSelection] = useState({});
  const table = useReactTable({
    data,
    columns,
    state: {
      rowSelection,
    },
    initialState: {
      pagination: {
        pageIndex: 0,
        pageSize: pageSize,
      },
    },
    enableRowSelection: true,
    getFilteredRowModel: getFilteredRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    onRowSelectionChange: setRowSelection,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFacetedRowModel: getFacetedRowModel(),
    getFacetedUniqueValues: getFacetedUniqueValues()
  });

  return (
    <>
      <div className="space-y-3">
        <DataTableFilterbar
          table={table}
          columnFilters={columnFilters}
          columnToSearch={columnToSearch}
          refreshData={refreshData}
          exportData={exportData} />
        <div className="relative overflow-hidden overflow-x-auto">
          <Table>
            <TableHead>
              {table.getHeaderGroups().map((headerGroup) => (
                <TableRow
                  key={headerGroup.id}
                  className="border-y border-gray-200 dark:border-gray-800"
                >
                  {headerGroup.headers.map((header) => (
                    <TableHeaderCell
                      key={header.id}
                      className={cx(
                        "whitespace-nowrap py-1 text-sm sm:text-xs",
                        header.column.columnDef.meta?.className,
                      )}
                    >
                      {flexRender(
                        header.column.columnDef.header,
                        header.getContext(),
                      )}
                    </TableHeaderCell>
                  ))}
                </TableRow>
              ))}
            </TableHead>
            <TableBody>
              {table.getRowModel().rows?.length ? (
                table.getRowModel().rows.map((row) => (
                  <TableRow
                    key={row.id}
                    onClick={() => row.toggleSelected(!row.getIsSelected())}
                    className="group select-none hover:bg-gray-50 hover:dark:bg-gray-900"
                  >
                    {row.getVisibleCells().map((cell, index) => (
                      <TableCell
                        key={cell.id}
                        className={cx(
                          row.getIsSelected()
                            ? "bg-gray-50 dark:bg-gray-900"
                            : "",
                          "relative whitespace-nowrap py-1 text-gray-600 first:w-10 dark:text-gray-400",
                          cell.column.columnDef.meta?.className,
                        )}
                      >
                        {index === 0 && row.getIsSelected() && (
                          <div className="absolute inset-y-0 left-0 w-0.5 bg-indigo-600 dark:bg-indigo-500" />
                        )}
                        {flexRender(
                          cell.column.columnDef.cell,
                          cell.getContext(),
                        )}
                      </TableCell>
                    ))}
                  </TableRow>
                ))
              ) : (
                <TableRow>
                  <TableCell
                    colSpan={columns.length}
                    className="h-24 text-center"
                  >
                    No results.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
          <DataTableBulkEditor
            table={table}
            rowSelection={rowSelection}
            delete={() => bulkDelete(table.getSelectedRowModel().rows.map(row => row.original))}
          />
        </div>
        <DataTablePagination table={table} pageSize={pageSize} />
      </div>
    </>
  );
}

const DataTableFilterbar = <TData,>({
  table, columnFilters, columnToSearch, refreshData, exportData
}: {
  table: TanstackTable<TData>;
  columnFilters?: Record<string, string>;
  columnToSearch?: string;
  refreshData: () => void;
  exportData: () => void;
}) => {
  const [searchTerm, setSearchTerm] = useState<string>("");
  const isFiltered = table.getState().columnFilters.length > 0;

  const debouncedSetFilterValue = useDebouncedCallback((value: string) => {
    columnToSearch && table.getColumn(columnToSearch)?.setFilterValue(value);
  }, 300);

  const handleSearchChange = (event: ChangeEvent<HTMLInputElement>) => {
    const value = event.target.value;
    setSearchTerm(value);
    debouncedSetFilterValue(value);
  };

  return (
    <div className="flex flex-wrap items-center justify-between gap-2 sm:gap-x-6">
      <div className="flex w-full flex-col gap-2 sm:w-fit sm:flex-row sm:items-center">
        {
          columnFilters && Object.keys(columnFilters).length > 0 && (
            Object.entries(columnFilters).map(([key, value]) => (
              <DataTableFilter
                key={key}
                column={table.getColumn(key)}
                title={value}
                options={
                  table.getColumn(key)?.getFacetedUniqueValues()
                    ? Array.from(table.getColumn(key)!.getFacetedUniqueValues()!.entries()).map(
                      ([value, _count]) => ({
                        label: value?.toString() ?? "",
                        value: value?.toString() ?? "",
                      })
                    )
                    : undefined
                }
                type="checkbox"
              />
            ))
          )
        }
        {columnToSearch && table.getColumn(columnToSearch)?.getIsVisible() && (
          <Searchbar
            type="search"
            placeholder="Search by title..."
            value={searchTerm}
            onChange={handleSearchChange}
            className="w-full sm:max-w-[250px] sm:[&>input]:h-[30px]"
          />
        )}
        {isFiltered && (
          <Button
            variant="ghost"
            onClick={() => table.resetColumnFilters()}
            className="border border-gray-200 px-2 font-semibold text-indigo-600 sm:border-none sm:py-1 dark:border-gray-800 dark:text-indigo-500"
          >
            Clear filters
          </Button>
        )}
      </div>
      <div className="flex items-center gap-2">
        <Button
          variant="secondary"
          className="hidden gap-x-2 px-2 py-1.5 text-sm sm:text-xs lg:flex"
          onClick={refreshData}
        >
          <RefreshCcw className="size-4 shrink-0" aria-hidden="true" />
          Refresh
        </Button>
        <Button
          variant="secondary"
          className="hidden gap-x-2 px-2 py-1.5 text-sm sm:text-xs lg:flex"
          onClick={exportData}
        >
          <RiDownloadLine className="size-4 shrink-0" aria-hidden="true" />
          Export
        </Button>
        <DataTableViewOptions table={table} />
      </div>
    </div>
  );
}

/**
 * Types for DataTableFilter
 */
export type ConditionFilter = {
  condition: string;
  value: [number | string, number | string];
};

type FilterType = "select" | "checkbox" | "number";
type FilterValues = string | string[] | ConditionFilter | undefined;

interface DataTableFilterOption {
  label: string;
  value: string;
}

interface DataTableFilterProps<TData, TValue> {
  column: Column<TData, TValue> | undefined;
  title?: string;
  options?: DataTableFilterOption[];
  type?: FilterType;
  formatter?: (value: any) => string;
}

/**
 * Helper: Renders filter labels for the filter button
 */
const ColumnFiltersLabel: React.FC<{
  columnFilterLabels?: string[];
  className?: string;
}> = ({ columnFilterLabels, className }) => {
  if (!columnFilterLabels || columnFilterLabels.length === 0) return null;
  if (columnFilterLabels.length < 3) {
    return (
      <span className={cx("truncate", className)}>
        {columnFilterLabels.map((value, index) => (
          <span
            key={value}
            className={cx("font-semibold text-indigo-600 dark:text-indigo-400")}
          >
            {value}
            {index < columnFilterLabels.length - 1 && ", "}
          </span>
        ))}
      </span>
    );
  }
  return (
    <span className={cx("font-semibold text-indigo-600 dark:text-indigo-400", className)}>
      {columnFilterLabels[0]} and {columnFilterLabels.length - 1} more
    </span>
  );
};

/**
 * Main: DataTableFilter
 * - Handles select, checkbox, and number filter types
 * - Self-contained and readable
 */
export function DataTableFilter<TData, TValue>({
  column,
  title,
  options,
  type = "select",
  formatter = (value) => value.toString(),
}: DataTableFilterProps<TData, TValue>) {
  // State for selected filter values
  const columnFilters = column?.getFilterValue() as FilterValues;
  const [selectedValues, setSelectedValues] = React.useState<FilterValues>(columnFilters);

  React.useEffect(() => {
    setSelectedValues(columnFilters);
  }, [columnFilters]);

  // Compute display labels for filter button
  const columnFilterLabels = React.useMemo(() => {
    if (!selectedValues) return undefined;
    if (Array.isArray(selectedValues)) return selectedValues.map((value) => formatter(value));
    if (typeof selectedValues === "string") return [formatter(selectedValues)];
    if (typeof selectedValues === "object" && "condition" in selectedValues) {
      const conditionLabel = options?.find(opt => opt.value === selectedValues.condition)?.label;
      if (!conditionLabel) return undefined;
      const [val0, val1] = selectedValues.value || ["", ""];
      if (!val0 && !val1) return [conditionLabel];
      if (!val1) return [`${conditionLabel} ${formatter(val0)}`];
      return [`${conditionLabel} ${formatter(val0)} and ${formatter(val1)}`];
    }
    return undefined;
  }, [selectedValues, options, formatter]);

  // Render filter input based on type
  function getDisplayedFilter() {
    switch (type) {
      case "select":
        return (
          <Select
            value={selectedValues as string}
            onValueChange={setSelectedValues}
          >
            <SelectTrigger className="mt-2 sm:py-1">
              <SelectValue placeholder="Select" />
            </SelectTrigger>
            <SelectContent>
              {options?.map((item) => (
                <SelectItem key={item.value} value={item.value}>
                  {item.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        );
      case "checkbox":
        return (
          <div className="mt-2 space-y-2 overflow-y-auto sm:max-h-36">
            {options?.map((option) => (
              <div key={option.label} className="flex items-center gap-2">
                <Checkbox
                  id={option.value}
                  checked={Array.isArray(selectedValues) && selectedValues.includes(option.value)}
                  onCheckedChange={(checked) => {
                    setSelectedValues((prev) => {
                      if (checked) {
                        return prev ? [...(prev as string[]), option.value] : [option.value];
                      } else {
                        return (prev as string[]).filter((value) => value !== option.value);
                      }
                    });
                  }}
                />
                <Label htmlFor={option.value} className="text-base sm:text-sm">
                  {option.label}
                </Label>
              </div>
            ))}
          </div>
        );
      case "number": {
        const isBetween = (selectedValues as ConditionFilter)?.condition === "is-between";
        return (
          <div className="space-y-2">
            <Select
              value={(selectedValues as ConditionFilter)?.condition}
              onValueChange={(value) => {
                setSelectedValues((prev) => ({
                  condition: value,
                  value: [value !== "" ? (prev as ConditionFilter)?.value?.[0] : "", ""],
                }));
              }}
            >
              <SelectTrigger className="mt-2 sm:py-1">
                <SelectValue placeholder="Select condition" />
              </SelectTrigger>
              <SelectContent>
                {options?.map((item) => (
                  <SelectItem key={item.value} value={item.value}>
                    {item.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <div className="flex w-full items-center gap-2">
              <RiCornerDownRightLine className="size-4 shrink-0 text-gray-500" aria-hidden="true" />
              <Input
                disabled={!(selectedValues as ConditionFilter)?.condition}
                type="number"
                placeholder="$0"
                className="sm:[&>input]:py-1"
                value={(selectedValues as ConditionFilter)?.value?.[0]}
                onChange={(e) => {
                  setSelectedValues((prev) => ({
                    condition: (prev as ConditionFilter)?.condition,
                    value: [e.target.value, isBetween ? (prev as ConditionFilter)?.value?.[1] : ""],
                  }));
                }}
              />
              {isBetween && (
                <>
                  <span className="text-xs font-medium text-gray-500">and</span>
                  <Input
                    disabled={!(selectedValues as ConditionFilter)?.condition}
                    type="number"
                    placeholder="$0"
                    className="sm:[&>input]:py-1"
                    value={(selectedValues as ConditionFilter)?.value?.[1]}
                    onChange={(e) => {
                      setSelectedValues((prev) => ({
                        condition: (prev as ConditionFilter)?.condition,
                        value: [
                          (prev as ConditionFilter)?.value?.[0],
                          e.target.value,
                        ],
                      }));
                    }}
                  />
                </>
              )}
            </div>
          </div>
        );
      }
      default:
        return null;
    }
  }

  // Reset logic for filter values
  function handleReset() {
    column?.setFilterValue("");
    setSelectedValues(
      type === "checkbox"
        ? []
        : type === "number"
          ? { condition: "", value: ["", ""] }
          : ""
    );
  }

  // Button style for filter popover
  const isActive = selectedValues && (
    (typeof selectedValues === "object" && "condition" in selectedValues && selectedValues.condition !== "") ||
    (typeof selectedValues === "string" && selectedValues !== "") ||
    (Array.isArray(selectedValues) && selectedValues.length > 0)
  );

  return (
    <Popover>
      <PopoverTrigger asChild>
        <button
          type="button"
          className={cx(
            "flex w-full items-center gap-x-1.5 whitespace-nowrap rounded-md border border-gray-300 px-2 py-1.5 font-medium text-gray-600 hover:bg-gray-50 sm:w-fit sm:text-xs dark:border-gray-700 dark:text-gray-400 hover:dark:bg-gray-900",
            isActive ? "" : "border-dashed",
            focusRing,
          )}
        >
          <span
            aria-hidden="true"
            onClick={(e) => {
              if (selectedValues) {
                e.stopPropagation();
                column?.setFilterValue("");
                setSelectedValues("");
              }
            }}
          >
            <RiAddLine
              className={cx(
                "-ml-px size-5 shrink-0 transition sm:size-4",
                isActive && "rotate-45 hover:text-red-500",
              )}
              aria-hidden="true"
            />
          </span>
          {columnFilterLabels && columnFilterLabels.length > 0 ? (
            <span>{title}</span>
          ) : (
            <span className="w-full text-left sm:w-fit">{title}</span>
          )}
          {columnFilterLabels && columnFilterLabels.length > 0 && (
            <span className="h-4 w-px bg-gray-300 dark:bg-gray-700" aria-hidden="true" />
          )}
          <ColumnFiltersLabel columnFilterLabels={columnFilterLabels} className="w-full text-left sm:w-fit" />
          <RiArrowDownSLine className="size-5 shrink-0 text-gray-500 sm:size-4" aria-hidden="true" />
        </button>
      </PopoverTrigger>
      <PopoverContent
        align="start"
        sideOffset={7}
        className="min-w-[calc(var(--radix-popover-trigger-width))] max-w-[calc(var(--radix-popover-trigger-width))] sm:min-w-56 sm:max-w-56"
        onInteractOutside={() => {
          if (
            !columnFilters ||
            (typeof columnFilters === "string" && columnFilters === "") ||
            (Array.isArray(columnFilters) && columnFilters.length === 0) ||
            (typeof columnFilters === "object" && "condition" in columnFilters && columnFilters.condition === "")
          ) {
            column?.setFilterValue("");
            setSelectedValues("");
          }
        }}
      >
        <form
          onSubmit={(e) => {
            e.preventDefault();
            column?.setFilterValue(selectedValues);
          }}
        >
          <div className="space-y-2">
            <div>
              <Label className="text-base font-medium sm:text-sm">Filter by {title}</Label>
              {getDisplayedFilter()}
            </div>
            <PopoverClose className="w-full" asChild>
              <Button type="submit" className="w-full sm:py-1">Apply</Button>
            </PopoverClose>
            {columnFilterLabels && columnFilterLabels.length > 0 && (
              <Button variant="secondary" className="w-full sm:py-1" type="button" onClick={handleReset}>
                Reset
              </Button>
            )}
          </div>
        </form>
      </PopoverContent>
    </Popover>
  );
}



type CleanupFn = () => void

type ItemEntry = { itemId: string; element: HTMLElement }

type ListContextValue = {
  getListLength: () => number
  registerItem: (entry: ItemEntry) => CleanupFn
  reorderItem: (args: {
    startIndex: number
    indexOfTarget: number
    closestEdgeOfTarget: Edge | null
  }) => void
  instanceId: symbol
}

const ListContext = React.createContext<ListContextValue | null>(null)

function useListContext() {
  const listContext = React.useContext(ListContext)
  invariant(listContext !== null)
  return listContext
}

type Item = {
  id: string
  label: string
}

const itemKey = Symbol("item")

type ItemData = {
  [itemKey]: true
  item: Item
  index: number
  instanceId: symbol
}

function getItemData({
  item,
  index,
  instanceId,
}: {
  item: Item
  index: number
  instanceId: symbol
}): ItemData {
  return {
    [itemKey]: true,
    item,
    index,
    instanceId,
  }
}

function isItemData(data: Record<string | symbol, unknown>): data is ItemData {
  return data[itemKey] === true
}

type DraggableState =
  | { type: "idle" }
  | { type: "preview"; container: HTMLElement }
  | { type: "dragging" }

const idleState: DraggableState = { type: "idle" }
const draggingState: DraggableState = { type: "dragging" }

function ListItem({
  item,
  index,
  column,
}: {
  item: Item
  index: number
  column: Column<any, unknown> | undefined
}) {
  const { registerItem, instanceId } = useListContext()

  const ref = React.useRef<HTMLDivElement>(null)
  const [closestEdge, setClosestEdge] = React.useState<Edge | null>(null)

  const dragHandleRef = React.useRef<HTMLButtonElement>(null)

  const [draggableState, setDraggableState] =
    React.useState<DraggableState>(idleState)

  React.useEffect(() => {
    const element = ref.current
    const dragHandle = dragHandleRef.current
    invariant(element)
    invariant(dragHandle)

    const data = getItemData({ item, index, instanceId })

    return combine(
      registerItem({ itemId: item.id, element }),
      draggable({
        element: dragHandle,
        getInitialData: () => data,
        onGenerateDragPreview({ nativeSetDragImage }) {
          setCustomNativeDragPreview({
            nativeSetDragImage,
            getOffset: pointerOutsideOfPreview({
              x: "10px",
              y: "10px",
            }),
            render({ container }) {
              setDraggableState({ type: "preview", container })

              return () => setDraggableState(draggingState)
            },
          })
        },
        onDragStart() {
          setDraggableState(draggingState)
        },
        onDrop() {
          setDraggableState(idleState)
        },
      }),
      dropTargetForElements({
        element,
        canDrop({ source }) {
          return (
            isItemData(source.data) && source.data.instanceId === instanceId
          )
        },
        getData({ input }) {
          return attachClosestEdge(data, {
            element,
            input,
            allowedEdges: ["top", "bottom"],
          })
        },
        onDrag({ self, source }) {
          const isSource = source.element === element
          if (isSource) {
            setClosestEdge(null)
            return
          }

          const closestEdge = extractClosestEdge(self.data)

          const sourceIndex = source.data.index
          invariant(typeof sourceIndex === "number")

          const isItemBeforeSource = index === sourceIndex - 1
          const isItemAfterSource = index === sourceIndex + 1

          const isDropIndicatorHidden =
            (isItemBeforeSource && closestEdge === "bottom") ||
            (isItemAfterSource && closestEdge === "top")

          if (isDropIndicatorHidden) {
            setClosestEdge(null)
            return
          }

          setClosestEdge(closestEdge)
        },
        onDragLeave() {
          setClosestEdge(null)
        },
        onDrop() {
          setClosestEdge(null)
        },
      }),
    )
  }, [instanceId, item, index, registerItem])

  return (
    <React.Fragment>
      <div ref={ref} className="relative border-b border-transparent">
        <div
          className={cx(
            "relative flex items-center justify-between gap-2",
            draggableState.type === "dragging" && "opacity-50",
          )}
        >
          <div className="flex items-center gap-2">
            <Checkbox
              checked={column?.getIsVisible()}
              onCheckedChange={() => column?.toggleVisibility()}
            />
            <span>{item.label}</span>
          </div>
          <Button
            aria-hidden="true"
            tabIndex={-1}
            variant="ghost"
            className="-mr-1 px-0 py-1"
            ref={dragHandleRef}
            aria-label={`Reorder ${item.label}`}
          >
            <RiDraggable className="size-5 text-gray-400 dark:text-gray-600" />
          </Button>
        </div>
        {closestEdge && <DropIndicator edge={closestEdge} gap="1px" />}
      </div>
      {draggableState.type === "preview" &&
        ReactDOM.createPortal(
          <div>{item.label}</div>,
          draggableState.container,
        )}
    </React.Fragment>
  )
}

function getItemRegistry() {
  const registry = new Map<string, HTMLElement>()

  function register({ itemId, element }: ItemEntry) {
    registry.set(itemId, element)

    return function unregister() {
      registry.delete(itemId)
    }
  }

  function getElement(itemId: string): HTMLElement | null {
    return registry.get(itemId) ?? null
  }

  return { register, getElement }
}

type ListState = {
  items: Item[]
  lastCardMoved: {
    item: Item
    previousIndex: number
    currentIndex: number
    numberOfItems: number
  } | null
}

interface DataTableViewOptionsProps<TData> {
  table: TanstackTable<TData>
}

export function DataTableViewOptions<TData>({ table }: DataTableViewOptionsProps<TData>) {
  const tableColumns: Item[] = table.getAllColumns().map((column) => ({
    id: column.id,
    label: column.columnDef.meta?.displayName as string,
  }))
  const [{ items, lastCardMoved }, setListState] = React.useState<ListState>({
    items: tableColumns,
    lastCardMoved: null,
  })
  const [registry] = React.useState(getItemRegistry)

  // Isolated instances of this component from one another
  const [instanceId] = React.useState(() => Symbol("instance-id"))

  React.useEffect(() => {
    table.setColumnOrder(items.map((item) => item.id))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [items])

  const reorderItem = React.useCallback(
    ({
      startIndex,
      indexOfTarget,
      closestEdgeOfTarget,
    }: {
      startIndex: number
      indexOfTarget: number
      closestEdgeOfTarget: Edge | null
    }) => {
      const finishIndex = getReorderDestinationIndex({
        startIndex,
        closestEdgeOfTarget,
        indexOfTarget,
        axis: "vertical",
      })

      if (finishIndex === startIndex) {
        return
      }

      setListState((listState) => {
        const item = listState.items[startIndex]

        return {
          items: reorder({
            list: listState.items,
            startIndex,
            finishIndex,
          }),
          lastCardMoved: {
            item,
            previousIndex: startIndex,
            currentIndex: finishIndex,
            numberOfItems: listState.items.length,
          },
        }
      })
    },
    [],
  )

  React.useEffect(() => {
    return monitorForElements({
      canMonitor({ source }) {
        return isItemData(source.data) && source.data.instanceId === instanceId
      },
      onDrop({ location, source }) {
        const target = location.current.dropTargets[0]
        if (!target) {
          return
        }

        const sourceData = source.data
        const targetData = target.data
        if (!isItemData(sourceData) || !isItemData(targetData)) {
          return
        }

        const indexOfTarget = items.findIndex(
          (item) => item.id === targetData.item.id,
        )
        if (indexOfTarget < 0) {
          return
        }

        const closestEdgeOfTarget = extractClosestEdge(targetData)

        reorderItem({
          startIndex: sourceData.index,
          indexOfTarget,
          closestEdgeOfTarget,
        })
      },
    })
  }, [instanceId, items, reorderItem])

  // once a drag is finished, we have some post drop actions to take
  React.useEffect(() => {
    if (lastCardMoved === null) {
      return
    }

    const { item, previousIndex, currentIndex, numberOfItems } = lastCardMoved
    const element = registry.getElement(item.id)
    if (element) {
      triggerPostMoveFlash(element)
    }

    liveRegion.announce(
      `You've moved ${item.label} from position ${previousIndex + 1
      } to position ${currentIndex + 1} of ${numberOfItems}.`,
    )
  }, [lastCardMoved, registry])

  // cleanup the live region when this component is finished
  React.useEffect(() => {
    return function cleanup() {
      liveRegion.cleanup()
    }
  }, [])

  const getListLength = React.useCallback(() => items.length, [items.length])

  const contextValue: ListContextValue = React.useMemo(() => {
    return {
      registerItem: registry.register,
      reorderItem,
      instanceId,
      getListLength,
    }
  }, [registry.register, reorderItem, instanceId, getListLength])

  return (
    <div>
      <div className="flex justify-center">
        <Popover>
          <PopoverTrigger asChild>
            <Button
              variant="secondary"
              className={cx(
                "ml-auto hidden gap-x-2 px-2 py-1.5 text-sm sm:text-xs lg:flex",
              )}
            >
              <RiEqualizer2Line className="size-4" aria-hidden="true" />
              View
            </Button>
          </PopoverTrigger>
          <PopoverContent
            align="end"
            sideOffset={7}
            className="z-50 w-fit space-y-2"
          >
            <Label className="font-medium">Display properties</Label>
            <ListContext.Provider value={contextValue}>
              <div className="flex flex-col">
                {items.map((item, index) => {
                  const column = table.getColumn(item.id)
                  if (!column) return null
                  return (
                    <div
                      key={column.id}
                      className={cx(!column.getCanHide() && "hidden")}
                    >
                      <ListItem column={column} item={item} index={index} />
                    </div>
                  )
                })}
              </div>
            </ListContext.Provider>
          </PopoverContent>
        </Popover>
      </div>
    </div>
  )
}

type DataTableBulkEditorProps<TData> = {
  table: TanstackTable<TData>
  rowSelection: RowSelectionState
  edit?: () => void;
  delete?: () => void;
}

function DataTableBulkEditor<TData>({
  table,
  rowSelection,
  edit,
  delete: del
}: DataTableBulkEditorProps<TData>) {
  const [open, setOpen] = useState(false);
  const handleDelete = () => setOpen(true);
  const handleClose = () => setOpen(false);
  const handleConfirmDelete = () => {
    del?.();
    setOpen(false);
    table.toggleAllRowsSelected(false);
  };
  const hasSelectedRows = Object.keys(rowSelection).length > 0;

  const deleteDialog = (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Confirm Bulk Delete</DialogTitle>
          <DialogDescription>
            Are you sure you want to delete {Object.keys(rowSelection).length} sessions? This action cannot be undone.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button variant="secondary" onClick={handleClose}>Cancel</Button>
          </DialogClose>
          <Button
            variant="destructive"
            onClick={handleConfirmDelete}
          >
            Delete
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );

  return (
    <>
      <CommandBar open={hasSelectedRows}>
        <CommandBarBar>
          <CommandBarValue>
            {Object.keys(rowSelection).length} selected
          </CommandBarValue>
          <CommandBarSeperator />
          {edit && (
            <CommandBarCommand
              label="Edit"
              action={edit}
              shortcut={{ shortcut: "e" }}
            />
          )}
          {edit && <CommandBarSeperator />}
          {del && (
            <CommandBarCommand
              label="Delete"
              action={handleDelete}
              shortcut={{ shortcut: "d" }}
            />
          )}
          {del && <CommandBarSeperator />}
          <CommandBarCommand
            label="Reset"
            action={() => {
              table.resetRowSelection()
            }}
            shortcut={{ shortcut: "Escape", label: "esc" }}
          />
        </CommandBarBar>
      </CommandBar>
      {deleteDialog}
    </>
  );
}

interface DataTablePaginationProps<TData> {
  table: TanstackTable<TData>
  pageSize: number
}

export function DataTablePagination<TData>({
  table,
  pageSize,
}: DataTablePaginationProps<TData>) {
  const paginationButtons = [
    {
      icon: RiArrowLeftDoubleLine,
      onClick: () => table.setPageIndex(0),
      disabled: !table.getCanPreviousPage(),
      srText: "First page",
      mobileView: "hidden sm:block",
    },
    {
      icon: RiArrowLeftSLine,
      onClick: () => table.previousPage(),
      disabled: !table.getCanPreviousPage(),
      srText: "Previous page",
      mobileView: "",
    },
    {
      icon: RiArrowRightSLine,
      onClick: () => table.nextPage(),
      disabled: !table.getCanNextPage(),
      srText: "Next page",
      mobileView: "",
    },
    {
      icon: RiArrowRightDoubleLine,
      onClick: () => table.setPageIndex(table.getPageCount() - 1),
      disabled: !table.getCanNextPage(),
      srText: "Last page",
      mobileView: "hidden sm:block",
    },
  ]

  const totalRows = table.getFilteredRowModel().rows.length
  const currentPage = table.getState().pagination.pageIndex
  const firstRowIndex = currentPage * pageSize + 1
  const lastRowIndex = Math.min(totalRows, firstRowIndex + pageSize - 1)

  return (
    <div className="flex items-center justify-between">
      <div className="text-sm tabular-nums text-gray-500">
        {table.getFilteredSelectedRowModel().rows.length} of {totalRows} row(s)
        selected.
      </div>
      <div className="flex items-center gap-x-6 lg:gap-x-8">
        <p className="hidden text-sm tabular-nums text-gray-500 sm:block">
          Showing{" "}
          <span className="font-medium text-gray-900 dark:text-gray-50">
            {firstRowIndex}-{lastRowIndex}
          </span>{" "}
          of{" "}
          <span className="font-medium text-gray-900 dark:text-gray-50">
            {totalRows}
          </span>
        </p>
        <div className="flex items-center gap-x-1.5">
          {paginationButtons.map((button, index) => (
            <Button
              key={index}
              variant="secondary"
              className={cx(button.mobileView, "p-1.5")}
              onClick={() => {
                button.onClick()
                table.resetRowSelection()
              }}
              disabled={button.disabled}
            >
              <span className="sr-only">{button.srText}</span>
              <button.icon className="size-4 shrink-0" aria-hidden="true" />
            </Button>
          ))}
        </div>
      </div>
    </div>
  )
}

interface DataTableColumnHeaderProps<TData, TValue>
  extends React.HTMLAttributes<HTMLDivElement> {
  column: Column<TData, TValue>
  title: string
}

export function DataTableColumnHeader<TData, TValue>({
  column,
  title,
  className,
}: DataTableColumnHeaderProps<TData, TValue>) {
  if (!column.getCanSort()) {
    return <div className={cx(className)}>{title}</div>
  }

  return (
    <div
      onClick={column.getToggleSortingHandler()}
      className={cx(
        column.columnDef.enableSorting === true
          ? "-mx-2 inline-flex cursor-pointer select-none items-center gap-2 rounded-md px-2 py-1 hover:bg-gray-50 hover:dark:bg-gray-900"
          : "",
      )}
    >
      <span>{title}</span>
      {column.getCanSort() ? (
        <div className="-space-y-2">
          <RiArrowUpSLine
            className={cx(
              "size-3.5 text-gray-900 dark:text-gray-50",
              column.getIsSorted() === "desc" ? "opacity-30" : "",
            )}
            aria-hidden="true"
          />
          <RiArrowDownSLine
            className={cx(
              "size-3.5 text-gray-900 dark:text-gray-50",
              column.getIsSorted() === "asc" ? "opacity-30" : "",
            )}
            aria-hidden="true"
          />
        </div>
      ) : null}
    </div>
  )
}


interface DataTableRowActionsProps<TData> {
  row: Row<TData>
  subject: string;
  add?: () => void;
  edit?: () => void;
  delete?: () => void;
}

export function DataTableRowActions<TData>({ row, subject, add, edit, delete: del }: DataTableRowActionsProps<TData>) {
  const [open, setOpen] = useState(false);
  const handleDelete = () => setOpen(true);
  const handleClose = () => setOpen(false);
  const handleConfirmDelete = () => {
    del?.();
    setOpen(false);
    row.toggleSelected(false);
  };

  const deleteDialog = (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Confirm Delete</DialogTitle>
          <DialogDescription>
            Are you sure you want to delete session
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
  );

  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            className="group aspect-square p-1.5 hover:border hover:border-gray-300 data-[state=open]:border-gray-300 data-[state=open]:bg-gray-50 hover:dark:border-gray-700 data-[state=open]:dark:border-gray-700 data-[state=open]:dark:bg-gray-900"
            onClick={e => e.stopPropagation()}
          >
            <RiMoreFill
              className="size-4 shrink-0 text-gray-500 group-hover:text-gray-700 group-data-[state=open]:text-gray-700 group-hover:dark:text-gray-300 group-data-[state=open]:dark:text-gray-300"
              aria-hidden="true"
            />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="min-w-40">
          {add && (
            <DropdownMenuItem onClick={add}>Add</DropdownMenuItem>
          )}
          {edit && (
            <DropdownMenuItem onClick={edit}>Edit</DropdownMenuItem>
          )}
          {del && (
            <DropdownMenuItem className="text-red-600 dark:text-red-500" onClick={handleDelete}>
              Delete
            </DropdownMenuItem>
          )}
        </DropdownMenuContent>
      </DropdownMenu>
      {deleteDialog}
    </>
  );
}

declare module "@tanstack/react-table" {
  interface ColumnMeta<TData, TValue> {
    className?: string
    displayName: string
  }
}