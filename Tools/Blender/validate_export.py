"""Validate the active Blender scene before exporting a fighter."""
import math
import re
import sys

import bpy


def validate(scene):
    errors = []
    rigs = [obj for obj in scene.objects if obj.type == "ARMATURE"]
    meshes = [obj for obj in scene.objects if obj.type == "MESH"]
    if len(rigs) != 1:
        errors.append(f"Expected one armature, found {len(rigs)}")
    if not meshes:
        errors.append("No mesh objects")
    if not math.isclose(scene.unit_settings.scale_length, 1.0):
        errors.append("Scene unit scale must be 1 metre")

    rig = rigs[0] if len(rigs) == 1 else None
    if rig:
        bones = rig.data.bones
        if sum(bone.use_deform for bone in bones) > 30:
            errors.append("More than 30 deform bones")
        if sum(bone.parent is None for bone in bones) != 1:
            errors.append("The armature must have one root")
        for bone in bones:
            if not re.fullmatch(r"[A-Z][A-Za-z0-9]*(?:_[A-Z][A-Za-z0-9]*)*(?:\.[LR])?", bone.name):
                errors.append(f"Unexpected bone name: {bone.name}")

    triangles = 0
    materials = set()
    for obj in rigs + meshes:
        if not obj.matrix_basis.is_identity:
            errors.append(f"{obj.name}: apply location, rotation, and scale")
    for mesh in meshes:
        mesh.data.calc_loop_triangles()
        triangles += len(mesh.data.loop_triangles)
        materials.update(slot.material for slot in mesh.material_slots if slot.material)
        if not mesh.data.uv_layers:
            errors.append(f"{mesh.name}: no UV map")
        if rig and not any(mod.type == "ARMATURE" and mod.object == rig for mod in mesh.modifiers):
            errors.append(f"{mesh.name}: no modifier bound to the fighter armature")
        for vertex in mesh.data.vertices:
            weights = [group.weight for group in vertex.groups if group.weight > 0.0001]
            if not 1 <= len(weights) <= 4 or not math.isclose(sum(weights), 1.0, abs_tol=0.001):
                errors.append(f"{mesh.name}: invalid skin weights at vertex {vertex.index}")
                break
    if triangles > 12000:
        errors.append(f"Triangle budget exceeded: {triangles} > 12000")
    if not 1 <= len(materials) <= 2:
        errors.append(f"Expected 1–2 materials, found {len(materials)}")
    return errors


if __name__ == "__main__":
    problems = validate(bpy.context.scene)
    if problems:
        sys.exit("\n".join(problems))
    print("Fighter export validation passed")
