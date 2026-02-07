shader_type canvas_item;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec4 fill_color : source_color = vec4(0.2, 0.9, 0.3, 0.9);
uniform vec4 empty_color : source_color = vec4(0.5, 0.5, 0.5, 0.3);
uniform float ring_radius : hint_range(0.0, 0.5) = 0.4;
uniform float ring_thickness : hint_range(0.0, 0.2) = 0.08;

void fragment() {
    vec2 uv = UV - vec2(0.5);
    float dist = length(uv);
    float angle = atan(uv.x, uv.y); // start from top
    float normalized_angle = (angle + PI) / (2.0 * PI);

    float inner = ring_radius - ring_thickness;
    float outer = ring_radius;

    // Anti-aliased ring mask
    float ring_mask = smoothstep(inner - 0.01, inner, dist) - smoothstep(outer, outer + 0.01, dist);

    // Fill based on progress
    float fill = step(normalized_angle, progress);
    vec4 color = mix(empty_color, fill_color, fill);

    COLOR = vec4(color.rgb, color.a * ring_mask);
}
