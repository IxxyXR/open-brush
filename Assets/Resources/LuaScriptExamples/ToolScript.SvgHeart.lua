﻿Settings = {previewType="quad"}

Parameters = {
    spacing={label="Point Spacing", type="float", min=0.1, max=1, default=0.1},
}

function OnTriggerReleased()
    infinitySvg = "M463.2 176.805a112 112 0 0 0-158.39 0l-48.57 48.568l-48.573-48.573a112 112 0 1 0 0 158.392l48.568-48.569l48.57 48.569A112 112 0 1 0 463.2 176.805ZM185.04 312.569a80 80 0 1 1 0-113.138l55.2 55.2v2.746Zm255.528 0a80 80 0 0 1-113.136 0l-55.2-55.2v-2.744l55.2-55.2a80 80 0 1 1 113.136 113.144Z"
    heartSvg = "M213.588,120.982L115,213.445l-98.588-92.463C-6.537,96.466-5.26,57.99,19.248,35.047l2.227-2.083 c24.51-22.942,62.984-21.674,85.934,2.842L115,43.709l7.592-7.903c22.949-24.516,61.424-25.784,85.936-2.842l2.227,2.083 C235.26,57.99,236.537,96.466,213.588,120.982z"
    local stroke = Path:FromSvgPath(heartSvg)
    stroke:Normalize(stroke, 2) -- Scale and center inside a 2x2 square
    stroke:Resample(spacing) -- Make the stroke evenly spaced
    lowest = path:FindMinimumX(path) -- Find the point with the lowest x value
    path:StartingFrom(lowest) -- Make it the new start point
    return stroke
end