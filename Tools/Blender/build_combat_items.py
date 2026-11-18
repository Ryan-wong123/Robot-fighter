"""Build the six pickup models and export each collection as a static FBX."""
from math import cos, pi, sin, tau
from pathlib import Path
import sys

import bmesh
import bpy
from mathutils import Vector

source, destination = map(Path, sys.argv[sys.argv.index("--") + 1:])
if source.exists():
    raise FileExistsError(source)
destination.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)

palette = {
    "Lemon": "FFC735", "Cream": "FFF0AD", "Stem": "76412C",
    "Cherry": "EB3857", "Wine": "832540", "Ivory": "FFF8DE",
    "Apricot": "F6A633", "Orange": "EF6025", "Ink": "2B2037",
    "Cyan": "39CDED", "Navy": "244A78", "Silver": "C1CEDF",
    "Bubblegum": "F35FA8", "Berry": "A42E74", "Lilac": "7974A5",
    "Slate": "464465", "Steel": "A8A7C2",
}


def linear(channel):
    return channel / 12.92 if channel <= .04045 else ((channel + .055) / 1.055) ** 2.4


materials = {}
for name, color in palette.items():
    mat = bpy.data.materials.new(f"{name}_{color}")
    rgb = tuple(linear(int(color[i:i+2], 16) / 255) for i in (0, 2, 4))
    mat.diffuse_color = (*rgb, 1)
    mat.use_nodes = True
    mat.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value = (*rgb, 1)
    materials[name] = mat


def finish(name, color, smooth=True):
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(materials[color])
    for face in obj.data.polygons:
        face.use_smooth = smooth
    return obj


def oval(name, color, center, scale, tilt=0):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=14, ring_count=8, location=center)
    obj = finish(name, color)
    obj.scale = scale
    obj.rotation_euler.y = tilt
    return obj


def bevel(obj, radius):
    modifier = obj.modifiers.new("Rounded edges", "BEVEL")
    modifier.width = radius
    modifier.segments = 3
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    modifier = obj.modifiers.new("Face normals", "WEIGHTED_NORMAL")
    bpy.ops.object.modifier_apply(modifier=modifier.name)


def box(name, color, center, scale, radius):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = finish(name, color)
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel(obj, radius)
    return obj


def cylinder(name, color, center, lower, upper, depth):
    bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=lower, radius2=upper, depth=depth, location=center)
    obj = finish(name, color)
    bevel(obj, .012)
    return obj


def peel():
    for arm in range(3):
        angle = arm * tau / 3 + .25
        vertices = []
        sections = [(0, .39, .06), (.12, .24, .19), (.32, .09, .24),
                    (.54, .02, .19), (.73, .04, .08), (.79, .09, .02)]
        for distance, height, width in sections:
            for side, thickness in [(-1, 0), (0, -.035), (1, 0), (0, .025)]:
                vertices.append((cos(angle)*distance - sin(angle)*width*side,
                                 sin(angle)*distance + cos(angle)*width*side, height+thickness))
        faces = [(3, 2, 1, 0)]
        for row in range(len(sections)-1):
            for col in range(4):
                i, j = 4*row + col, 4*row + (col+1)%4
                faces.append((i, j, j+4, i+4))
        faces.append((20, 21, 22, 23))
        mesh = bpy.data.meshes.new("Peel ribbon")
        mesh.from_pydata(vertices, [], faces)
        surface = bmesh.new()
        surface.from_mesh(mesh)
        bmesh.ops.recalc_face_normals(surface, faces=list(surface.faces))
        surface.to_mesh(mesh)
        surface.free()
        mesh.update()
        obj = bpy.data.objects.new("Curled peel flap", mesh)
        bpy.context.collection.objects.link(obj)
        obj.data.materials.append(materials["Lemon"])
        obj.data.materials.append(materials["Cream"])
        for face in obj.data.polygons:
            face.use_smooth = True
            face.material_index = int(face.index > 0 and (face.index - 1) % 4 >= 2)
        bpy.context.view_layer.objects.active = obj
        modifier = obj.modifiers.new("Soft peel curl", "SUBSURF")
        modifier.levels = 2
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    cylinder("Brown stem", "Stem", (0, 0, .44), .105, .07, .21)


def glove():
    oval("Padded knuckles", "Cherry", (.07, 0, .26), (.46, .31, .37), -.15)
    oval("Curled thumb", "Cherry", (-.27, -.24, .16), (.18, .15, .24), -.45)
    box("Wrist cuff", "Wine", (-.04, .01, -.07), (.53, .51, .27), .105)
    box("Wrist strap", "Ivory", (-.04, -.25, -.065), (.34, .055, .12), .024)
    oval("Knuckle panel", "Wine", (.15, -.276, .24), (.235, .05, .095), -.14)


def chicken():
    oval("Squeaky body", "Apricot", (-.04, 0, -.05), (.37, .245, .29))
    oval("Long neck", "Apricot", (.24, 0, .25), (.105, .11, .32), .28)
    oval("Head", "Apricot", (.32, 0, .51), (.18, .14, .16))
    oval("Beak", "Orange", (.49, -.03, .495), (.148, .09, .062), -.14)
    oval("Wattle", "Cherry", (.402, -.015, .39), (.073, .067, .077))
    for x, z in ((.24, .651), (.32, .675), (.395, .639)):
        oval("Comb", "Cherry", (x, 0, z), (.062, .06, .10))
    for side in (-1, 1):
        oval("Wing", "Lemon", (-.11, .20*side, -.04), (.24, .07, .135), -.3)
        oval("Eye", "Ivory", (.342, .126*side, .547), (.063, .032, .074))
        oval("Pupil", "Ink", (.363, .153*side, .551), (.025, .012, .037))
        oval("Foot", "Orange", (.13, .11*side, -.33), (.145, .085, .065))
    oval("Tail", "Apricot", (-.36, 0, .025), (.19, .125, .12), .52)


def plunger():
    cylinder("Suction cup", "Cyan", (0, 0, -.20), .36, .15, .29)
    cylinder("Rubber lip", "Navy", (0, 0, -.34), .38, .37, .075)
    cylinder("Handle shaft", "Silver", (0, 0, .17), .074, .074, .52)
    cylinder("Rocket casing", "Cyan", (0, 0, .16), .15, .13, .24)
    cylinder("Grip", "Navy", (0, 0, .42), .116, .116, .16)
    for angle in (0, tau/3, 2*tau/3):
        fin = box("Rocket fin", "Orange", (.195*cos(angle), .195*sin(angle), .035),
                  (.22, .075, .21), .025)
        fin.rotation_euler.z = angle


def cushion():
    oval("Welded edge", "Berry", (0, 0, 0), (.47, .34, .106))
    oval("Inflated shell", "Bubblegum", (0, 0, .034), (.44, .31, .132))
    box("Air neck", "Berry", (.47, 0, .007), (.28, .19, .095), .04)
    box("Mouthpiece", "Bubblegum", (.585, 0, .02), (.11, .23, .092), .035)
    for offset, length in ((-.105, .15), (0, .22), (.105, .14)):
        oval("Air burst emblem", "Ivory", (-.05, offset, .155),
             (length/2, .024, .012), 0)


def anvil():
    box("Splayed foot", "Slate", (-.035, 0, -.235), (.69, .43, .16), .055)
    box("Waist", "Lilac", (-.045, 0, -.044), (.43, .30, .32), .095)
    box("Strike plate", "Steel", (-.075, 0, .166), (.70, .39, .155), .042)
    horn = cylinder("Rounded horn", "Lilac", (.385, 0, .12), .176, .026, .45)
    horn.rotation_euler.y = pi/2
    box("Face inset", "Slate", (-.06, -.158, -.04), (.22, .027, .085), .012)


for key, build, width in (
    ("BananaPeel", peel, .58), ("FoamGlove", glove, .56), ("RubberChicken", chicken, .58),
    ("RocketPlunger", plunger, .55), ("WhoopeeCushion", cushion, .63), ("Anvil", anvil, .66),
):
    existing = set(bpy.context.scene.objects)
    build()
    parts = [obj for obj in bpy.context.scene.objects if obj not in existing]
    bpy.ops.object.select_all(action="DESELECT")
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = key
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bounds = list(map(Vector, obj.bound_box))
    low = Vector(tuple(min(point[i] for point in bounds) for i in range(3)))
    high = Vector(tuple(max(point[i] for point in bounds) for i in range(3)))
    center = (low + high) / 2
    size = high - low
    for vertex in obj.data.vertices:
        vertex.co = (vertex.co - center) * (width/max(size))
    obj.data.update()
    obj.data.calc_loop_triangles()
    triangles = len(obj.data.loop_triangles)
    if not 0 < triangles <= 5000 or obj.scale != Vector((1, 1, 1)):
        raise ValueError(f"{key}: invalid pickup geometry or scale ({triangles} triangles)")
    print(f"{key}: {triangles} triangles, {width:.2f} m maximum extent")
    bpy.ops.export_scene.fbx(filepath=str((destination / f"{key}.fbx").resolve()),
                             use_selection=True, object_types={"MESH"}, axis_forward="-Z",
                             axis_up="Y", add_leaf_bones=False)
    obj.location.x = (len(bpy.context.scene.objects) - 1) * .85

source.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(source.resolve()))
