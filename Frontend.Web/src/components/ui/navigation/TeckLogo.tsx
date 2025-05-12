import teckLogo from "@/public/teck.png"
import Image from "next/image"

export function TeckLogoDesktop() {
    return (
        <Image src={teckLogo} alt="logo" width={1822} height={1092} />
    )
}